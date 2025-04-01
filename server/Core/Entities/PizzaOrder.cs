using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class PizzaOrder
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Content { get; set; } = null!;

    public string? OrderNumber { get; set; }

    public List<string>? Toppings { get; set; }
}
