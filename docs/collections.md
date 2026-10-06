# Collections

## Collections of Simple Types

You can use the `RuleForEach` method to apply the same rule to multiple items in a collection:

```csharp
public class Person 
{
  public List<string> AddressLines { get; set; } = new List<string>();
}
```

```csharp
public class PersonValidator : AbstractValidator<Person> 
{
  public PersonValidator() 
  {
    RuleForEach(x => x.AddressLines).NotNull();
  }
}
```

The above rule will run a NotNull check against each item in the `AddressLines` collection.

As of version 8.5, if you want to access the index of the collection element that caused the validation failure, you can use the special `{CollectionIndex}` placeholder:

```csharp
public class PersonValidator : AbstractValidator<Person> 
{
  public PersonValidator() 
  {
    RuleForEach(x => x.AddressLines).NotNull().WithMessage("Address {CollectionIndex} is required.");
  }
}
```

## Collections of Complex Types

You can also combine `RuleForEach` with `SetValidator` when the collection is of another complex objects. For example:

```csharp
public class Customer 
{
  public List<Order> Orders { get; set; } = new List<Order>();
}

public class Order 
{
  public double Total { get; set; }
}
```

```csharp
public class OrderValidator : AbstractValidator<Order> 
{
  public OrderValidator() 
  {
    RuleFor(x => x.Total).GreaterThan(0);
  }
}

public class CustomerValidator : AbstractValidator<Customer> 
{
  public CustomerValidator() 
  {
    RuleForEach(x => x.Orders).SetValidator(new OrderValidator());
  }
}
```

Alternatively, as of FluentValidation 8.5, you can also define rules for child collection elements in-line using the `ChildRules` method:

```csharp
public class CustomerValidator : AbstractValidator<Customer> 
{
  public CustomerValidator() 
  {
    RuleForEach(x => x.Orders).ChildRules(order => 
    {
      order.RuleFor(x => x.Total).GreaterThan(0);
    });
  }
}
```

You can optionally include or exclude certain items in the collection from being validated by using the `Where` or `WhereAsync` methods. Note this must come directly after the call to `RuleForEach`:

```csharp
RuleForEach(x => x.Orders)
  .Where(x => x.Cost != null)
  .SetValidator(new OrderValidator());
```

### Original indexes and element identity

Every element processed by `RuleForEach` has an explicit identity: its index in the *original* collection. Filtering with `Where`/`WhereAsync` does not renumber the remaining elements - errors are always associated with the original index, never with the compressed position an element happens to hold within the filtered sequence. The same applies when the property name is customized with `OverridePropertyName`, and to the `{CollectionIndex}` placeholder.

For example, given three orders where the middle one is excluded by the filter and the third one fails validation, the error is reported against index `[2]`, not `[1]`:

```csharp
public class CustomerValidator : AbstractValidator<Customer>
{
  public CustomerValidator()
  {
    RuleForEach(x => x.Orders)
      .Where(order => order != null) // filters out the middle (null) order
      .SetValidator(new OrderValidator());
  }
}

// customer.Orders = [ validOrder, null, invalidOrder ]
var result = validator.Validate(customer);
// result.Errors[0].PropertyName == "Orders[2].Total"  (not "Orders[1].Total")
```

If you need the index inside the filter itself, use the overloads of `Where`/`WhereAsync` that receive the element's original index:

```csharp
RuleForEach(x => x.Orders)
  .Where((order, index) => index != 1) // skip the second order by its original index
  .SetValidator(new OrderValidator());
```

Child validators attached with `SetValidator` (or `ChildRules`) inherit the parent property path (so error property names look like `Orders[2].Total`) and can read the root `RootContextData` dictionary.

### Collection snapshots

By default (`CollectionSnapshotPolicy.None`) `RuleForEach` enumerates the live collection while validation runs. If user code modifies the collection from within a validator, some collection types silently shift their remaining elements, which can attach errors to the wrong item. Calling `UseCollectionSnapshot` makes the rule validate a read-only snapshot of the collection that is materialized before validation begins, so every element keeps the identity (original index) it had when validation started:

```csharp
RuleForEach(x => x.Orders)
  .UseCollectionSnapshot(CollectionSnapshotPolicy.Snapshot)
  .Must(order => order.Total > 0);
```

With `CollectionSnapshotPolicy.SnapshotAndRejectMutation` (the default when calling `UseCollectionSnapshot()` without arguments) the source collection is additionally checked for modification once validation of the collection completes. If a modification is detected (for collections that expose a count), an `InvalidOperationException` is thrown and the potentially mixed validation result is rejected instead of being returned:

```csharp
RuleForEach(x => x.Orders)
  .UseCollectionSnapshot(CollectionSnapshotPolicy.SnapshotAndRejectMutation)
  .SetValidator(new OrderValidator());
```

Empty (and null) collections are valid under every policy and produce no errors.

### Running the collection tests

The behavior described above is covered by a focused test fixture. To run only these tests:

```bash
dotnet test src/FluentValidation.Tests/FluentValidation.Tests.csproj -f net10.0 --filter "FullyQualifiedName~CollectionSnapshotTests"
```

To also run the pre-existing `RuleForEach`/collection suites (confirming the default rule chain is unchanged):

```bash
dotnet test src/FluentValidation.Tests/FluentValidation.Tests.csproj -f net10.0 --filter "FullyQualifiedName~ForEachRuleTests|FullyQualifiedName~CollectionValidatorWithParentTests|FullyQualifiedName~CollectionSnapshotTests"
```

As of version 8.2, an alternative to using `RuleForEach` is to call `ForEach` as part of a regular `RuleFor`. With this approach you can combine rules that act upon the entire collection with rules which act upon individual elements within the collection. For example, imagine you have the following 2 rules:

```csharp
// This rule acts on the whole collection (using RuleFor)
RuleFor(x => x.Orders)
  .Must(x => x.Count <= 10).WithMessage("No more than 10 orders are allowed");

// This rule acts on each individual element (using RuleForEach)
RuleForEach(x => x.Orders)
  .Must(order => order.Total > 0).WithMessage("Orders must have a total of more than 0")
```

The above 2 rules could be re-written as:

```csharp
RuleFor(x => x.Orders)
  .Must(x => x.Count <= 10).WithMessage("No more than 10 orders are allowed")
  .ForEach(orderRule => 
  {
    orderRule.Must(order => order.Total > 0).WithMessage("Orders must have a total of more than 0")
  });
```

We recommend using 2 separate rules as this is clearer and easier to read, but the option of combining them is available with the `ForEach` method.
