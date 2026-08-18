using CineTicket.Web.Helpers;
using CineTicket.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CineTicket.Web.Services;

// Corre en segundo plano mientras la aplicacion esta encendida: cada 5 minutos
// revisa si hay compras "PENDIENTE" que ya superaron el tiempo de reserva (15 min)
// y las cancela automaticamente, liberando esos asientos para otros clientes.
public class LimpiezaComprasPendientesService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(5);

    public LimpiezaComprasPendientesService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CineTicketContext>();

                var limite = DateTime.Now.AddMinutes(-DisponibilidadHelper.MINUTOS_EXPIRACION_PENDIENTE);
                var vencidas = await db.Ventas
                    .Where(v => v.Estado == "PENDIENTE" && v.FechaVenta < limite)
                    .Include(v => v.DetalleVenta)
                    .ToListAsync(stoppingToken);

                foreach (var venta in vencidas)
                {
                    db.DetalleVenta.RemoveRange(venta.DetalleVenta);
                    db.Ventas.Remove(venta);
                }

                if (vencidas.Any())
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch
            {
                // Si una pasada falla, no se detiene el servicio: se reintenta en el siguiente ciclo.
            }

            await Task.Delay(_intervalo, stoppingToken);
        }
    }
}