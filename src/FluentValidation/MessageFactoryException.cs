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
using Results;

/// <summary>
/// Represents an exception that was thrown by an error message factory callback
/// (for example a <c>WithMessage</c> callback) after a rule had already failed.
/// </summary>
/// <remarks>
/// This is only created when <see cref="ValidatorConfiguration.AdaptMessageFactoryExceptions"/>
/// is enabled. When the option is disabled (the default), the original exception
/// thrown by the message factory is propagated unchanged.
/// The adapted instance is attached to the resulting <see cref="ValidationFailure"/>
/// via its <see cref="ValidationFailure.CustomState"/> property, preserving the
/// rule path, the original failure and the callback cause (as <see cref="Exception.InnerException"/>).
/// </remarks>
public class MessageFactoryException : Exception {
	/// <summary>
	/// Creates a new instance of the <see cref="MessageFactoryException"/> class.
	/// </summary>
	/// <param name="propertyPath">The path of the property/rule whose message factory threw.</param>
	/// <param name="innerException">The exception thrown by the message factory callback.</param>
	public MessageFactoryException(string propertyPath, Exception innerException)
		: base($"The error message factory for property '{propertyPath}' threw an exception: {innerException?.Message}", innerException) {
		PropertyPath = propertyPath;
	}

	/// <summary>
	/// The path of the property/rule whose message factory threw.
	/// </summary>
	public string PropertyPath { get; }

	/// <summary>
	/// The original validation failure that was being constructed when the
	/// message factory threw. This is the same instance that is added to the
	/// validation results.
	/// </summary>
	public ValidationFailure Failure { get; internal set; }
}
