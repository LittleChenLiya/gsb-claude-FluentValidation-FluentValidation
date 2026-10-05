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
using Results;

public static partial class DefaultValidatorExtensions {
	/// <summary>
	/// Validates the specified instance and produces an explicit rule execution report.
	/// The report records each rule by its own path together with the category of its
	/// outcome (for example <see cref="RuleExecutionCategory.Succeeded"/>,
	/// <see cref="RuleExecutionCategory.Failed"/>, <see cref="RuleExecutionCategory.SkippedByAsyncCondition"/>,
	/// <see cref="RuleExecutionCategory.StoppedByCascade"/> or <see cref="RuleExecutionCategory.NotExecuted"/>).
	/// Dependent rules are only entered when their parent rule actually succeeds; dependent
	/// rules that never ran are recorded as <see cref="RuleExecutionCategory.NotExecuted"/>
	/// rather than being treated as passed.
	/// </summary>
	/// <remarks>
	/// The contained <see cref="ValidationExecutionReport.Result"/> is identical to the result
	/// of a regular call to <see cref="IValidator{T}.Validate(T)"/> for the same instance:
	/// enabling the report does not invoke validators any additional times. Each call uses a
	/// fresh context, so reports are never chained across calls and concurrent calls are isolated.
	/// </remarks>
	/// <param name="validator">The validator.</param>
	/// <param name="instance">The instance to validate.</param>
	/// <typeparam name="T">The type of the instance being validated.</typeparam>
	/// <returns>The validation result together with the rule execution report.</returns>
	public static ValidationExecutionReport ValidateWithExecutionReport<T>(this IValidator<T> validator, T instance) {
		ArgumentNullException.ThrowIfNull(validator);
		var context = new ValidationContext<T>(instance, null, ValidatorOptions.Global.ValidatorSelectors.DefaultValidatorSelectorFactory());
		context.EnableRuleExecutionReport();
		var result = validator.Validate(context);
		return new ValidationExecutionReport(result, context.RuleExecutionLog);
	}

	/// <summary>
	/// Validates the specified instance asynchronously and produces an explicit rule execution
	/// report. The report records each rule by its own path together with the category of its
	/// outcome. Dependent rules are only entered when their parent rule actually succeeds;
	/// dependent rules that never ran are recorded as <see cref="RuleExecutionCategory.NotExecuted"/>
	/// rather than being treated as passed.
	/// </summary>
	/// <remarks>
	/// The contained <see cref="ValidationExecutionReport.Result"/> is identical to the result
	/// of a regular call to <see cref="IValidator{T}.ValidateAsync(T, CancellationToken)"/> for the
	/// same instance: enabling the report does not invoke validators any additional times. Each
	/// call uses a fresh context, so reports are never chained across calls and concurrent calls
	/// are isolated.
	/// </remarks>
	/// <param name="validator">The validator.</param>
	/// <param name="instance">The instance to validate.</param>
	/// <param name="cancellation">Cancellation token.</param>
	/// <typeparam name="T">The type of the instance being validated.</typeparam>
	/// <returns>The validation result together with the rule execution report.</returns>
	public static async Task<ValidationExecutionReport> ValidateWithExecutionReportAsync<T>(this IValidator<T> validator, T instance, CancellationToken cancellation = default) {
		ArgumentNullException.ThrowIfNull(validator);
		var context = new ValidationContext<T>(instance, null, ValidatorOptions.Global.ValidatorSelectors.DefaultValidatorSelectorFactory());
		context.EnableRuleExecutionReport();
		var result = await validator.ValidateAsync(context, cancellation);
		return new ValidationExecutionReport(result, context.RuleExecutionLog);
	}
}
