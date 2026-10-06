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
/// Specifies how rules should cascade when one fails.
/// </summary>
public enum CascadeMode {
	/// <summary>
	/// When a rule/validator fails, execution continues to the next rule/validator.
	/// For more information, see the methods/properties that accept this enum as a parameter.
	/// </summary>
	Continue = 0,

	/// <summary>
	/// When a rule/validator fails, validation is stopped for the current rule/validator.
	/// For more information, see the methods/properties that accept this enum as a parameter.
	/// </summary>
	Stop = 2, // Note Stop is 2 for backwards compatibility. The StopOnFirstFailure option was 1, which was removed in 12.0. This is explicitly set to 2 to prevent it from being automatically renumbered to 1.
}

/// <summary>
/// Specifies where a When/Unless condition should be applied
/// </summary>
public enum ApplyConditionTo {
	/// <summary>
	/// Applies the condition to all validators declared so far in the chain.
	/// </summary>
	AllValidators,
	/// <summary>
	/// Applies the condition to the current validator only.
	/// </summary>
	CurrentValidator
}

/// <summary>
/// Specifies the severity of a rule.
/// </summary>
public enum Severity {
	/// <summary>
	/// Error
	/// </summary>
	Error,
	/// <summary>
	/// Warning
	/// </summary>
	Warning,
	/// <summary>
	/// Info
	/// </summary>
	Info
}

/// <summary>
/// Specifies how a rule created with <c>RuleForEach</c> interacts with the source
/// collection while validation is running.
/// </summary>
public enum CollectionSnapshotPolicy {
	/// <summary>
	/// The collection is enumerated directly while validation runs (the default).
	/// Each element is still identified by its original index, but if user code
	/// modifies the collection during validation the behaviour is determined by
	/// the collection's own enumerator (many collections throw, others silently
	/// shift elements, which can attach errors to the wrong item).
	/// </summary>
	None = 0,

	/// <summary>
	/// A read-only snapshot of the collection is materialized before validation begins
	/// and validation runs against that snapshot. Every element keeps the identity
	/// (original index) it had in the source collection, so user code modifying the
	/// collection during validation cannot cause errors to be attached to the wrong item.
	/// </summary>
	Snapshot = 1,

	/// <summary>
	/// A read-only snapshot is materialized (as with <see cref="CollectionSnapshotPolicy.Snapshot"/>)
	/// and, in addition, the source collection is checked for modification when validation
	/// of the collection completes. If a modification is detected, an
	/// <see cref="InvalidOperationException"/> is thrown and the (potentially mixed)
	/// validation result is rejected instead of being returned to the caller.
	/// Modification can only be detected for collections that expose a count
	/// (<see cref="System.Collections.Generic.ICollection{T}"/> or
	/// <see cref="System.Collections.ICollection"/>).
	/// </summary>
	SnapshotAndRejectMutation = 2,
}
