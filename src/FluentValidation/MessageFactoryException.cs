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

/// <summary>
/// Represents an exception that was thrown by an error message factory (for example a
/// <c>WithMessage</c> callback) after a rule had already failed.
/// This is only produced when <see cref="ValidatorConfiguration.AdaptMessageFactoryExceptions"/>
/// is enabled. The original callback exception is preserved as the <see cref="Exception.InnerException"/>.
/// </summary>
[Serializable]
public class MessageFactoryException : Exception {
	/// <summary>
	/// Creates a new instance of the <see cref="MessageFactoryException"/> class.
	/// </summary>
	/// <param name="propertyPath">The path of the property whose rule failed.</param>
	/// <param name="attemptedValue">The property value that caused the validation failure.</param>
	/// <param name="innerException">The exception thrown by the message factory callback.</param>
	public MessageFactoryException(string propertyPath, object attemptedValue, Exception innerException)
		: base($"An exception was thrown by the error message factory for property '{propertyPath}'. See the InnerException for details.", innerException) {
		PropertyPath = propertyPath;
		AttemptedValue = attemptedValue;
	}

	/// <summary>
	/// The path of the property whose rule failed.
	/// </summary>
	public string PropertyPath { get; }

	/// <summary>
	/// The property value that caused the validation failure.
	/// </summary>
	public object AttemptedValue { get; }
}
