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
/// Tests that RuleForEach gives every element an explicit, stable identity (its original
/// index): Where-filtered items do not compress the indices written to the client, child
/// validators inherit the parent path, user mutation of the collection cannot re-attach
/// errors to the wrong item, and empty collections produce no spurious errors.
/// </summary>
public class CollectionIndexIdentityTests {
	// Three-item fixture: the middle item is filtered out, the third item is the one that fails.
	private static Person CreateThreeItemPerson() => new Person {
		Orders = new List<Order> {
			new Order { ProductName = "First", Amount = 1 },
			new Order { ProductName = "FilteredOut", Amount = 1 },
			new Order { ProductName = "Third", Amount = 0 },
		}
	};

	[Fact]
	public void Where_filter_preserves_original_index_of_failing_item() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Where(o => o.ProductName != "FilteredOut")
			.Must(o => o.Amount != 0);

		var result = validator.Validate(CreateThreeItemPerson());

		// The middle item was filtered out, but the failure on the third item
		// must still be reported against its original index [2], not the compressed [1].
		result.Errors.Single().PropertyName.ShouldEqual("Orders[2]");
	}

	[Fact]
	public async Task Where_filter_preserves_original_index_of_failing_item_async() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.WhereAsync(o => Task.FromResult(o.ProductName != "FilteredOut"))
			.MustAsync((o, ct) => Task.FromResult(o.Amount != 0));

		var result = await validator.ValidateAsync(CreateThreeItemPerson());

		result.Errors.Single().PropertyName.ShouldEqual("Orders[2]");
	}

	[Fact]
	public void Indexed_Where_receives_original_indices() {
		var seenIndices = new List<int>();
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Where((order, index) => {
				seenIndices.Add(index);
				return true;
			})
			.Must(o => true);

		var result = validator.Validate(CreateThreeItemPerson());

		result.IsValid.ShouldBeTrue();
		Assert.Equal(new[] { 0, 1, 2 }, seenIndices);
	}

	[Fact]
	public void Indexed_Where_filtering_keeps_original_index_in_error() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Where((order, index) => index != 1)
			.Must(o => o.Amount != 0);

		var result = validator.Validate(CreateThreeItemPerson());

		result.Errors.Single().PropertyName.ShouldEqual("Orders[2]");
	}

	[Fact]
	public async Task Indexed_WhereAsync_filtering_keeps_original_index_in_error() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.WhereAsync((order, index) => Task.FromResult(index != 1))
			.MustAsync((o, ct) => Task.FromResult(o.Amount != 0));

		var result = await validator.ValidateAsync(CreateThreeItemPerson());

		result.Errors.Single().PropertyName.ShouldEqual("Orders[2]");
	}

	[Fact]
	public void CollectionIndex_placeholder_uses_original_index_after_filtering() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Where(o => o.ProductName != "FilteredOut")
			.Must(o => o.Amount != 0)
			.WithMessage("Failed at {CollectionIndex}");

		var result = validator.Validate(CreateThreeItemPerson());

		result.Errors.Single().ErrorMessage.ShouldEqual("Failed at 2");
	}

	[Fact]
	public void Child_validator_path_contains_original_index_after_filtering() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Where(o => o.ProductName != "FilteredOut")
			.SetValidator(new AmountValidator());

		var result = validator.Validate(CreateThreeItemPerson());

		// The child validator inherits the parent path including the original index.
		result.Errors.Single().PropertyName.ShouldEqual("Orders[2].Amount");
	}

	[Fact]
	public void Collection_modified_during_validation_keeps_errors_on_original_items() {
		var person = CreateThreeItemPerson();
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Must((p, order) => {
				// User code mutates the source collection mid-validation.
				if (order.ProductName == "First") {
					p.Orders.RemoveAt(0);
				}
				return order.Amount != 0;
			});

		var result = validator.Validate(person);

		// The default UseSnapshot policy validates against the original snapshot, so the
		// failure is still attached to the third item's original index rather than being
		// shifted onto the wrong item.
		result.Errors.Single().PropertyName.ShouldEqual("Orders[2]");
	}

	[Fact]
	public void Collection_modified_during_validation_throws_when_policy_is_ThrowOnMutation() {
		var person = CreateThreeItemPerson();
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Must((p, order) => {
				if (order.ProductName == "First") {
					p.Orders.RemoveAt(0);
				}
				return true;
			});

		var previous = ValidatorOptions.Global.CollectionMutationPolicy;
		ValidatorOptions.Global.CollectionMutationPolicy = CollectionMutationPolicy.ThrowOnMutation;
		try {
			Assert.Throws<InvalidOperationException>(() => validator.Validate(person));
		}
		finally {
			ValidatorOptions.Global.CollectionMutationPolicy = previous;
		}
	}

	[Fact]
	public void Empty_collection_is_valid_and_produces_no_errors() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Where(o => o.ProductName != null)
			.Must(o => o.Amount != 0);

		var result = validator.Validate(new Person { Orders = new List<Order>() });

		result.IsValid.ShouldBeTrue();
		Assert.Empty(result.Errors);
	}

	[Fact]
	public void Empty_collection_with_child_validator_produces_no_errors() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders).SetValidator(new AmountValidator());

		var result = validator.Validate(new Person { Orders = new List<Order>() });

		result.IsValid.ShouldBeTrue();
		Assert.Empty(result.Errors);
	}

	[Fact]
	public void OverridePropertyName_preserves_original_index() {
		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders)
			.Where(o => o.ProductName != "FilteredOut")
			.Must(o => o.Amount != 0)
			.OverridePropertyName("Lines");

		var result = validator.Validate(CreateThreeItemPerson());

		// A custom property name must not lose the original index either.
		result.Errors.Single().PropertyName.ShouldEqual("Lines[2]");
	}

	[Fact]
	public void Child_validator_inherits_readonly_view_of_root_context_data() {
		object observedValue = null;
		bool writeThrew = false;

		var childValidator = new InlineValidator<Order>();
		childValidator.RuleFor(x => x.ProductName)
			.Custom((productName, ctx) => {
				observedValue = ctx.RootContextData["marker"];
				try {
					ctx.RootContextData["marker"] = "changed-by-child";
				}
				catch (NotSupportedException) {
					writeThrew = true;
				}
			});

		var validator = new InlineValidator<Person>();
		validator.RuleForEach(x => x.Orders).SetValidator(childValidator);

		var context = new ValidationContext<Person>(CreateThreeItemPerson());
		context.RootContextData["marker"] = "original";

		validator.Validate(context);

		// The child sees the parent's data (live view) but cannot modify it,
		// and the parent's data is left untouched.
		observedValue.ShouldEqual("original");
		writeThrew.ShouldBeTrue();
		context.RootContextData["marker"].ShouldEqual("original");
	}

	private class AmountValidator : AbstractValidator<Order> {
		public AmountValidator() {
			RuleFor(x => x.Amount).NotEqual(0);
		}
	}
}
