using CineTicket.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Helpers;

public static class DisponibilidadHelper
{
    public const int MINUTOS_EXPIRACION_PENDIENTE = 15;

    // Un asiento se considera ocupado si tiene una venta CONFIRMADA,
    // o una venta PENDIENTE que todavia no vencio (dentro de los 15 minutos de "reserva").
    public static IQueryable<int> AsientosOcupados(CineTicketContext db, int idFuncion)
    {
        var limite = DateTime.Now.AddMinutes(-MINUTOS_EXPIRACION_PENDIENTE);
        return db.DetalleVenta
            .Where(d => d.IdFuncion == idFuncion &&
                (d.IdVentaNavigation.Estado == "CONFIRMADA" ||
                 (d.IdVentaNavigation.Estado == "PENDIENTE" && d.IdVentaNavigation.FechaVenta >= limite)))
            .Select(d => d.IdAsiento);
    }
}