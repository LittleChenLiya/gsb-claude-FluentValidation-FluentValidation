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

namespace FluentValidation.Results;

/// <summary>
/// A single entry in a rule execution report. A record contains only the path of the
/// rule (its actual identity within the object graph) and the category of its outcome.
/// </summary>
public sealed class RuleExecutionRecord {
	/// <summary>
	/// Creates a new rule execution record.
	/// </summary>
	/// <param name="path">The path of the property the rule was defined against.</param>
	/// <param name="category">The category of the rule's execution outcome.</param>
	public RuleExecutionRecord(string path, RuleExecutionCategory category) {
		Path = path;
		Category = category;
	}

	/// <summary>
	/// The path of the property the rule was defined against.
	/// </summary>
	public string Path { get; }

	/// <summary>
	/// The category of the rule's execution outcome.
	/// </summary>
	public RuleExecutionCategory Category { get; }

	/// <inheritdoc />
	public override string ToString() => $"{Path}: {Category}";
}
