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

## Index stability, filtering and element identity

`RuleForEach` gives every element an explicit, stable identity: its original index within the source collection. When `Where`/`WhereAsync` filters items out, errors raised for the remaining items still carry their *original* indices - the compressed position within the filtered sequence is never written to the client.

For example, with three orders where the middle one is filtered out and the third one fails:

```csharp
public class CustomerValidator : AbstractValidator<Customer>
{
  public CustomerValidator()
  {
    RuleForEach(x => x.Orders)
      .Where(order => order.ProductName != "FilteredOut")
      .Must(order => order.Amount != 0);
  }
}
```

```csharp
var customer = new Customer
{
  Orders = new List<Order>
  {
    new Order { ProductName = "First", Amount = 1 },       // index 0 - valid
    new Order { ProductName = "FilteredOut", Amount = 1 }, // index 1 - skipped by Where
    new Order { ProductName = "Third", Amount = 0 },       // index 2 - fails
  }
};

var result = validator.Validate(customer);
// result.Errors[0].PropertyName is "Orders[2]", not "Orders[1]".
```

If you need the element's identity inside the filter itself, use the `Where`/`WhereAsync` overloads that receive the original index:

```csharp
RuleForEach(x => x.Orders)
  .Where((order, originalIndex) => originalIndex % 2 == 0)
  .SetValidator(new OrderValidator());
```

The same original index is used everywhere: in the property path (`Orders[2]`), in the `{CollectionIndex}` message placeholder, in `OverrideIndexer` callbacks, and when a custom property name is set with `OverridePropertyName` (eg `Lines[2]`). Child validators invoked with `SetValidator` inherit the parent's path, so errors appear as eg `Orders[2].Amount`, and they receive a live, read-only view of the parent's `RootContextData`.

Validation runs against a snapshot of the collection taken when the rule starts. If user code modifies the collection whilst validation is in progress (for example inside a `Custom` validator), errors can never be attached to the wrong item, because pre- and post-mutation views are never mixed. If you would rather reject such modifications outright, set the policy to throw:

```csharp
ValidatorOptions.Global.CollectionMutationPolicy = CollectionMutationPolicy.ThrowOnMutation;
```

An empty (or null) collection is valid and produces no errors.

The behaviour described above is covered by a targeted test fixture, which can be run on its own with:

```
dotnet test src/FluentValidation.Tests/FluentValidation.Tests.csproj -f net10.0 --filter "FullyQualifiedName~CollectionIndexIdentityTests"
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
