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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class MessageFactoryExceptionTester : IDisposable {

	public MessageFactoryExceptionTester() {
		ValidatorOptions.Global.AdaptMessageFactoryExceptions = true;
	}

	public void Dispose() {
		ValidatorOptions.Global.AdaptMessageFactoryExceptions = false;
	}

	[Fact]
	public void Two_rules_message_factory_throws_adapts_failure_preserves_path_chain_and_counts() {
		int surnameExecutions = 0;
		int forenameExecutions = 0;

		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname)
			.Must(x => { surnameExecutions++; return false; })
			.WithMessage(x => throw new InvalidOperationException("Surname message factory blew up"));
		validator.RuleFor(x => x.Forename)
			.Must(x => { forenameExecutions++; return false; })
			.WithMessage("Forename is invalid");

		var result = validator.Validate(new Person { Surname = "foo", Forename = "bar" });

		// The exception must not be treated as a successful validation.
		result.IsValid.ShouldBeFalse();
		result.Errors.Count.ShouldEqual(2);

		// Rule path, original failure and callback cause are preserved.
		var adapted = result.Errors[0];
		adapted.PropertyName.ShouldEqual("Surname");
		adapted.AttemptedValue.ShouldEqual("foo");
		var exception = Assert.IsType<MessageFactoryException>(adapted.CustomState);
		exception.PropertyPath.ShouldEqual("Surname");
		exception.Failure.ShouldBeTheSameAs(adapted);
		var cause = Assert.IsType<InvalidOperationException>(exception.InnerException);
		cause.Message.ShouldEqual("Surname message factory blew up");
		adapted.ErrorMessage.ShouldEqual(exception.Message);
		Assert.Contains("Surname message factory blew up", adapted.ErrorMessage);

		// Other rules continue per the original cascade behaviour and normal messages are unaffected.
		result.Errors[1].PropertyName.ShouldEqual("Forename");
		result.Errors[1].ErrorMessage.ShouldEqual("Forename is invalid");

		// Each validator executed exactly once.
		surnameExecutions.ShouldEqual(1);
		forenameExecutions.ShouldEqual(1);
	}

	[Fact]
	public void Cascade_stop_still_applies_when_message_factory_throws() {
		int firstExecutions = 0;
		int secondExecutions = 0;

		var validator = new InlineValidator<Person>();
		validator.RuleLevelCascadeMode = CascadeMode.Stop;
		validator.RuleFor(x => x.Surname)
			.Must(x => { firstExecutions++; return false; })
			.WithMessage(x => throw new InvalidOperationException("boom"))
			.Must(x => { secondExecutions++; return false; })
			.WithMessage("second component");

		var result = validator.Validate(new Person { Surname = "foo" });

		result.IsValid.ShouldBeFalse();
		result.Errors.Count.ShouldEqual(1);
		Assert.IsType<MessageFactoryException>(result.Errors[0].CustomState);
		firstExecutions.ShouldEqual(1);
		secondExecutions.ShouldEqual(0);
	}

	[Fact]
	public async Task Async_validator_is_not_reexecuted_when_message_factory_throws() {
		int executions = 0;

		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname)
			.MustAsync((x, ct) => { executions++; return Task.FromResult(false); })
			.WithMessage(x => throw new InvalidOperationException("async boom"));

		var result = await validator.ValidateAsync(new Person { Surname = "foo" }, CancellationToken.None);

		executions.ShouldEqual(1);
		result.IsValid.ShouldBeFalse();
		result.Errors.Count.ShouldEqual(1);
		result.Errors[0].PropertyName.ShouldEqual("Surname");
		var exception = Assert.IsType<MessageFactoryException>(result.Errors[0].CustomState);
		exception.InnerException.Message.ShouldEqual("async boom");
	}

	[Fact]
	public void When_option_disabled_original_exception_propagates() {
		ValidatorOptions.Global.AdaptMessageFactoryExceptions = false;

		var validator = new InlineValidator<Person>();
		validator.RuleFor(x => x.Surname)
			.Must(x => false)
			.WithMessage(x => throw new InvalidOperationException("boom"));

		var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(new Person { Surname = "foo" }));
		ex.Message.ShouldEqual("boom");
	}
}
