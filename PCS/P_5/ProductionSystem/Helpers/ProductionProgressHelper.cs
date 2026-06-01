using ProductionSystem.Models;

namespace ProductionSystem.Helpers;

public static class ProductionProgressHelper
{
    /// <summary>
    /// Доля выполнения заказа по расписанию (0–100) от StartDate до EstimatedEndDate.
    /// </summary>
    public static int CalculatePercent(WorkOrder order, DateTime? now = null)
    {
        var moment = now ?? DateTime.Now;

        if (order.Status == WorkOrder.StatusCompleted)
        {
            return 100;
        }

        if (order.Status != WorkOrder.StatusInProgress)
        {
            return order.ProgressPercent;
        }

        var total = order.EstimatedEndDate - order.StartDate;
        if (total <= TimeSpan.Zero)
        {
            return moment >= order.EstimatedEndDate ? 100 : 0;
        }

        var elapsed = moment - order.StartDate;
        if (elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        if (elapsed >= total)
        {
            return 100;
        }

        var percent = (int)Math.Floor(elapsed.TotalSeconds / total.TotalSeconds * 100);
        return Math.Clamp(percent, 0, 99);
    }
}
