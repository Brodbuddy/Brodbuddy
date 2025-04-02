using System;
using System.Collections.Generic;

namespace Core.Entities;

public partial class Onetimepassword
{
    public Guid Id { get; set; }

    public int Code { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Expiresat { get; set; }

    public bool? Isused { get; set; }
}
