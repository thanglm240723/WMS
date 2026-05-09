using System;
using System.Collections.Generic;

namespace warehouseManagement.Models;

public partial class OutboundItem
{
    public int Id { get; set; }

    public int OutboundRequestId { get; set; }

    public int ProductId { get; set; }

    public decimal Quantity { get; set; }

    public decimal? PickedQuantity { get; set; }

    public int? BinId { get; set; }

    public string? LineNote { get; set; }

    public int? UnitId { get; set; }

    public virtual OutboundRequest OutboundRequest { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual Bin? Bin { get; set; }
    public virtual Unit? Unit { get; set; }
}
