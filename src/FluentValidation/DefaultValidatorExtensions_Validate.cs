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

namespace FluentValidation;

using System;
using System.Threading;
using System.Threading.Tasks;
using Internal;
using Results;

public static partial class DefaultValidatorExtensions {

	/// <summary>
	/// Validates the specified instance using a combination of extra options
	/// </summary>
	/// <param name="validator">The validator</param>
	/// <param name="instance">The instance to validate</param>
	/// <param name="options">Callback to configure additional options</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static ValidationResult Validate<T>(this IValidator<T> validator, T instance, Action<ValidationStrategy<T>> options)
		=> validator.Validate(ValidationContext<T>.CreateWithOptions(instance, options));

	/// <summary>
	/// Validates the specified instance using a combination of extra options
	/// </summary>
	/// <param name="validator">The validator</param>
	/// <param name="instance">The instance to validate</param>
	/// <param name="cancellation">Cancellation token</param>
	/// <param name="options">Callback to configure additional options</param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static Task<ValidationResult> ValidateAsync<T>(this IValidator<T> validator, T instance, Action<ValidationStrategy<T>> options, CancellationToken cancellation = default)
		=> validator.ValidateAsync(ValidationContext<T>.CreateWithOptions(instance, options), cancellation);

	/// <summary>
	/// Performs validation and then throws an exception if validation fails.
	/// This method is a shortcut for: Validate(instance, options => options.ThrowOnFailures());
	/// </summary>
	/// <param name="validator">The validator this method is extending.</param>
	/// <param name="instance">The instance of the type we are validating.</param>
	public static void ValidateAndThrow<T>(this IValidator<T> validator, T instance) {
		validator.Validate(instance, options => {
			options.ThrowOnFailures();
		});
	}

	/// <summary>
	/// Performs validation asynchronously and then throws an exception if validation fails.
	/// This method is a shortcut for: ValidateAsync(instance, options => options.ThrowOnFailures());
	/// </summary>
	/// <param name="validator">The validator this method is extending.</param>
	/// <param name="instance">The instance of the type we are validating.</param>
	/// <param name="cancellationToken"></param>
	public static async Task ValidateAndThrowAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellationToken = default) {
		await validator.ValidateAsync(instance, options => {
			options.ThrowOnFailures();
		}, cancellationToken);
	}

	/// <summary>
	/// Validates the specified instance and returns both the validation result and an explicit
	/// report of which rules were executed (and which were not, for example dependent rules
	/// whose parent rule did not succeed). The <see cref="ValidationResult"/> is identical to
	/// the one produced by a regular call to Validate, and no additional validator invocations
	/// are performed. The report is scoped to this call and is never shared between calls.
	/// </summary>
	/// <param name="validator">The validator this method is extending.</param>
	/// <param name="instance">The instance of the type we are validating.</param>
	/// <returns>A tuple of the validation result and the rule execution report.</returns>
	public static (ValidationResult Result, RuleExecutionReport Report) ValidateWithExecutionReport<T>(this IValidator<T> validator, T instance) {
		var context = new ValidationContext<T>(instance);
		var result = validator.Validate(context);
		return (result, context.ExecutionReport);
	}

	/// <summary>
	/// Validates the specified instance asynchronously and returns both the validation result and
	/// an explicit report of which rules were executed (and which were not, for example dependent
	/// rules whose parent rule did not succeed). The <see cref="ValidationResult"/> is identical to
	/// the one produced by a regular call to ValidateAsync, and no additional validator invocations
	/// are performed. The report is scoped to this call and is never shared between calls.
	/// </summary>
	/// <param name="validator">The validator this method is extending.</param>
	/// <param name="instance">The instance of the type we are validating.</param>
	/// <param name="cancellation">Cancellation token</param>
	/// <returns>A tuple of the validation result and the rule execution report.</returns>
	public static async Task<(ValidationResult Result, RuleExecutionReport Report)> ValidateWithExecutionReportAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellation = default) {
		var context = new ValidationContext<T>(instance);
		var result = await validator.ValidateAsync(context, cancellation);
		return (result, context.ExecutionReport);
	}
}
