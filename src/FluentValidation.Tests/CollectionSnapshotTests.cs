#region License

// Copyright (c) .NET Foundation and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// The latest version of this file can be found at https://github.com/FluentValidation/FluentValidation

#endregion

namespace FluentValidation.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Tests for explicit element identity (original index) in RuleForEach:
/// Where-filtered collections keep the original index in error property names;
/// index-aware Where/WhereAsync overloads receive the original index;
/// custom property names keep the original index;
/// child validators inherit the parent path and RootContextData;
/// the collection snapshot policy prevents user modifications from attaching
/// errors to the wrong item;
/// empty collections are valid and produce no spurious errors.
/// </summary>
public class CollectionSnapshotTests {

	private static Person ThreeOrdersWithNullMiddle() {
		return new Person {
			Orders = new List<Order> {
				new Order { ProductName = "Valid1", Amount = 1 },
				null,
				new Order { ProductName = null, Amount = 2 }, // fails ProductName NotEmpty
			},
		};
	}

	public class OrderProductNameValidator : AbstractValidator<Order> {
		public OrderProductNameValidator() {
			RuleFor(x => x.ProductName).NotEmpty();
		}
	}

	[Fact]
	public void Filtered_collection_error_keeps_original_index() {
		// Three items; the middle one is filtered out. The third item fails and the
		// error must be reported against its original index [2], not the compressed
		// position [1] it holds within the filtered sequence.
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.Where(order => order != null)
				.SetValidator(new OrderProductNameValidator())
		};

		var result = validator.Validate(ThreeOrdersWithNullMiddle());

		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Orders[2].ProductName");
	}

	[Fact]
	public void Filtered_collection_CollectionIndex_placeholder_uses_original_index() {
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.Where(order => order != null)
				.Must(order => order.ProductName != null)
				.WithMessage("Failed at index {CollectionIndex}")
		};

		var result = validator.Validate(ThreeOrdersWithNullMiddle());

		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Orders[2]");
		result.Errors[0].ErrorMessage.ShouldEqual("Failed at index 2");
	}

	[Fact]
	public void Index_aware_filter_receives_original_indexes() {
		var seenIndexes = new List<int>();

		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.Where((order, index) => {
					seenIndexes.Add(index);
					return true;
				})
				.NotNull()
		};

		validator.Validate(ThreeOrdersWithNullMiddle());

		// All three elements are seen with their original indexes 0, 1, 2.
		seenIndexes.ShouldEqual(new[] { 0, 1, 2 });
	}

	[Fact]
	public void Index_aware_filter_can_filter_by_original_index() {
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.Where((order, index) => index != 1) // filter out the middle item by identity
				.Must(order => order != null && order.ProductName != null)
		};

		var result = validator.Validate(ThreeOrdersWithNullMiddle());

		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Orders[2]");
	}

	[Fact]
	public async Task Index_aware_async_filter_receives_original_indexes() {
		var seenIndexes = new List<int>();

		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.WhereAsync((order, index) => {
					seenIndexes.Add(index);
					return Task.FromResult(true);
				})
				.NotNull()
		};

		await validator.ValidateAsync(ThreeOrdersWithNullMiddle());

		seenIndexes.ShouldEqual(new[] { 0, 1, 2 });
	}

	[Fact]
	public void Index_aware_async_filter_throws_when_invoked_synchronously() {
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.WhereAsync((order, index) => Task.FromResult(true))
				.NotNull()
		};

		Assert.Throws<AsyncValidatorInvokedSynchronouslyException>(() => validator.Validate(ThreeOrdersWithNullMiddle()));
	}

	[Fact]
	public void Custom_property_name_keeps_original_index_after_filtering() {
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.Where(order => order != null)
				.SetValidator(new OrderProductNameValidator())
				.OverridePropertyName("Orders2")
		};

		var result = validator.Validate(ThreeOrdersWithNullMiddle());

		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Orders2[2].ProductName");
	}

	[Fact]
	public void Child_validator_inherits_parent_path_and_root_context_data() {
		var childValidator = new InlineValidator<Order> {
			v => v.RuleFor(x => x.ProductName)
				.Must((order, productName, ctx) => ctx.RootContextData.ContainsKey("flag") && productName != null)
				.WithMessage("missing flag or name")
		};

		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.Where(order => order != null)
				.SetValidator(childValidator)
		};

		var person = ThreeOrdersWithNullMiddle();
		var context = new ValidationContext<Person>(person);
		context.RootContextData["flag"] = true;

		var result = validator.Validate(context);

		// The child validator could read the inherited RootContextData, so the only
		// failure is the third item with null ProductName, at its original index.
		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Orders[2].ProductName");
	}

	[Fact]
	public void Snapshot_policy_preserves_original_indexes_when_collection_is_modified() {
		var person = new Person {
			Orders = new List<Order> {
				new Order { ProductName = null },
				new Order { ProductName = null },
				new Order { ProductName = null },
			},
		};
		bool removed = false;

		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.UseCollectionSnapshot(CollectionSnapshotPolicy.Snapshot)
				.Must(order => {
					// User code modifies the source collection mid-validation.
					if (!removed) {
						person.Orders.RemoveAt(0);
						removed = true;
					}
					return false; // every element fails
				})
		};

		var result = validator.Validate(person);

		// Errors are attached to the original identities, not to shifted positions.
		result.Errors.Select(e => e.PropertyName).ShouldEqual(new[] { "Orders[0]", "Orders[1]", "Orders[2]" });
	}

	[Fact]
	public void Reject_mutation_policy_throws_when_collection_is_modified() {
		var person = new Person {
			Orders = new List<Order> {
				new Order { ProductName = "a" },
				new Order { ProductName = "b" },
				new Order { ProductName = "c" },
			},
		};
		bool removed = false;

		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.UseCollectionSnapshot(CollectionSnapshotPolicy.SnapshotAndRejectMutation)
				.Must(order => {
					if (!removed) {
						person.Orders.RemoveAt(0);
						removed = true;
					}
					return true;
				})
		};

		var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(person));
		Assert.Contains("Orders", ex.Message);
		Assert.Contains("modified", ex.Message);
	}

	[Fact]
	public async Task Reject_mutation_policy_throws_when_collection_is_modified_async() {
		var person = new Person {
			Orders = new List<Order> {
				new Order { ProductName = "a" },
				new Order { ProductName = "b" },
			},
		};
		bool removed = false;

		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.UseCollectionSnapshot(CollectionSnapshotPolicy.SnapshotAndRejectMutation)
				.MustAsync((order, ct) => {
					if (!removed) {
						person.Orders.RemoveAt(0);
						removed = true;
					}
					return Task.FromResult(true);
				})
		};

		await Assert.ThrowsAsync<InvalidOperationException>(() => validator.ValidateAsync(person));
	}

	[Fact]
	public void Reject_mutation_policy_does_not_throw_when_collection_is_unmodified() {
		var person = ThreeOrdersWithNullMiddle();

		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.UseCollectionSnapshot(CollectionSnapshotPolicy.SnapshotAndRejectMutation)
				.Where(order => order != null)
				.SetValidator(new OrderProductNameValidator())
		};

		var result = validator.Validate(person);

		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Orders[2].ProductName");
	}

	[Fact]
	public void Empty_collection_is_valid_with_default_policy() {
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders).NotNull()
		};

		var result = validator.Validate(new Person { Orders = new List<Order>() });

		result.IsValid.ShouldBeTrue();
		result.Errors.Count.ShouldEqual(0);
	}

	[Fact]
	public void Empty_collection_is_valid_with_snapshot_policy() {
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.UseCollectionSnapshot()
				.NotNull()
		};

		var result = validator.Validate(new Person { Orders = new List<Order>() });

		result.IsValid.ShouldBeTrue();
		result.Errors.Count.ShouldEqual(0);
	}

	[Fact]
	public void Null_collection_produces_no_errors_with_snapshot_policy() {
		var validator = new TestValidator {
			v => v.RuleForEach(x => x.Orders)
				.UseCollectionSnapshot()
				.NotNull()
		};

		var result = validator.Validate(new Person { Orders = null });

		result.IsValid.ShouldBeTrue();
		result.Errors.Count.ShouldEqual(0);
	}
}
