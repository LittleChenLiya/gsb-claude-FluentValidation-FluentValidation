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
using System.Threading.Tasks;
using Xunit;

public class MessageFactoryExceptionTester : IDisposable {
	private static readonly InvalidOperationException CallbackCause = new("Message factory blew up");

	public MessageFactoryExceptionTester() {
		ValidatorOptions.Global.AdaptMessageFactoryExceptions = true;
	}

	public void Dispose() {
		ValidatorOptions.Global.AdaptMessageFactoryExceptions = false;
	}

	private static string ThrowingMessageFactory(Person person, string value) => throw CallbackCause;

	[Fact]
	public void Adapted_failure_preserves_path_original_failure_and_callback_cause() {
		int factoryCalls = 0;
		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname).NotNull().WithMessage((person, value) => {
			factoryCalls++;
			return ThrowingMessageFactory(person, value);
		});
		validator.RuleFor(x => x.Forename).NotNull().WithMessage("normal message");

		var result = validator.Validate(new Person());

		// The exception must not be counted as a validation pass: both rules failed.
		result.IsValid.ShouldBeFalse();
		result.Errors.Count.ShouldEqual(2);

		// First rule: adapted failure preserving path, original failure and callback cause.
		var adapted = result.Errors[0];
		adapted.PropertyName.ShouldEqual("Surname");
		adapted.MessageFactoryException.ShouldNotBeNull();
		adapted.MessageFactoryException.PropertyPath.ShouldEqual("Surname");
		// Error chain: MessageFactoryException -> original callback exception.
		adapted.MessageFactoryException.InnerException.ShouldBeTheSameAs(CallbackCause);
		adapted.ErrorMessage.ShouldEqual(adapted.MessageFactoryException.Message);

		// The message factory was only invoked once (no re-execution).
		factoryCalls.ShouldEqual(1);

		// Second rule continues as normal with its regular message.
		var normal = result.Errors[1];
		normal.PropertyName.ShouldEqual("Forename");
		normal.ErrorMessage.ShouldEqual("normal message");
		normal.MessageFactoryException.ShouldBeNull();
	}

	[Fact]
	public void Cascade_stop_still_stops_when_message_factory_throws() {
		int secondValidatorCalls = 0;
		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname)
			.Cascade(CascadeMode.Stop)
			.NotNull().WithMessage((person, value) => ThrowingMessageFactory(person, value))
			.Must(surname => {
				secondValidatorCalls++;
				return true;
			});

		var result = validator.Validate(new Person());

		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].MessageFactoryException.ShouldNotBeNull();
		// CascadeMode.Stop: the second component in the chain was never executed.
		secondValidatorCalls.ShouldEqual(0);
	}

	[Fact]
	public void Cascade_continue_runs_remaining_components_when_message_factory_throws() {
		int secondValidatorCalls = 0;
		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname)
			.Cascade(CascadeMode.Continue)
			.NotNull().WithMessage((person, value) => ThrowingMessageFactory(person, value))
			.Must(surname => {
				secondValidatorCalls++;
				return false;
			}).WithMessage("must failed");

		var result = validator.Validate(new Person());

		result.Errors.Count.ShouldEqual(2);
		result.Errors[0].MessageFactoryException.ShouldNotBeNull();
		result.Errors[1].ErrorMessage.ShouldEqual("must failed");
		result.Errors[1].MessageFactoryException.ShouldBeNull();
		secondValidatorCalls.ShouldEqual(1);
	}

	[Fact]
	public async Task Async_validator_is_not_reexecuted_when_message_factory_throws() {
		int validatorCalls = 0;
		int factoryCalls = 0;
		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname)
			.MustAsync((surname, token) => {
				validatorCalls++;
				return Task.FromResult(false);
			})
			.WithMessage((person, value) => {
				factoryCalls++;
				return ThrowingMessageFactory(person, value);
			});

		var result = await validator.ValidateAsync(new Person());

		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Surname");
		result.Errors[0].MessageFactoryException.ShouldNotBeNull();
		result.Errors[0].MessageFactoryException.InnerException.ShouldBeTheSameAs(CallbackCause);
		// The async validator and the message factory each ran exactly once.
		validatorCalls.ShouldEqual(1);
		factoryCalls.ShouldEqual(1);
	}

	[Fact]
	public void Exception_propagates_unchanged_when_adapter_not_enabled() {
		ValidatorOptions.Global.AdaptMessageFactoryExceptions = false;

		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname).NotNull().WithMessage((person, value) => ThrowingMessageFactory(person, value));

		// Original interface behaviour: the callback exception propagates to the caller.
		var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(new Person()));
		ex.ShouldBeTheSameAs(CallbackCause);
	}
}
