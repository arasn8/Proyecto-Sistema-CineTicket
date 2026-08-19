using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineTicket.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Generos",
                columns: table => new
                {
                    IdGenero = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Generos__0F83498816E1D3E0", x => x.IdGenero);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreRol = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__2A49584CD53478DA", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    IdSala = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Capacidad = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Salas__A04F9B3BE89125DA", x => x.IdSala);
                });

            migrationBuilder.CreateTable(
                name: "Peliculas",
                columns: table => new
                {
                    IdPelicula = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Titulo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    Sinopsis = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
                    DuracionMin = table.Column<int>(type: "int", nullable: false),
                    Clasificacion = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    IdGenero = table.Column<int>(type: "int", nullable: false),
                    ImagenUrl = table.Column<string>(type: "varchar(300)", unicode: false, maxLength: 300, nullable: true),
                    Estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Idioma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Pelicula__60537FD0B402A86C", x => x.IdPelicula);
                    table.ForeignKey(
                        name: "FK__Peliculas__IdGen__412EB0B6",
                        column: x => x.IdGenero,
                        principalTable: "Generos",
                        principalColumn: "IdGenero");
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombres = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Correo = table.Column<string>(type: "varchar(150)", unicode: false, maxLength: 150, nullable: false),
                    Clave = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    IdRol = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CodigoReset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodigoResetExpira = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Usuarios__5B65BF974C3CA82F", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK__Usuarios__IdRol__3A81B327",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "IdRol");
                });

            migrationBuilder.CreateTable(
                name: "Asientos",
                columns: table => new
                {
                    IdAsiento = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdSala = table.Column<int>(type: "int", nullable: false),
                    Fila = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Asientos__F5678721D0B785FF", x => x.IdAsiento);
                    table.ForeignKey(
                        name: "FK__Asientos__IdSala__46E78A0C",
                        column: x => x.IdSala,
                        principalTable: "Salas",
                        principalColumn: "IdSala");
                });

            migrationBuilder.CreateTable(
                name: "Funciones",
                columns: table => new
                {
                    IdFuncion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdPelicula = table.Column<int>(type: "int", nullable: false),
                    IdSala = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time", nullable: false),
                    PrecioEntrada = table.Column<decimal>(type: "decimal(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Funcione__7D4132883E33300A", x => x.IdFuncion);
                    table.ForeignKey(
                        name: "FK__Funciones__IdPel__49C3F6B7",
                        column: x => x.IdPelicula,
                        principalTable: "Peliculas",
                        principalColumn: "IdPelicula");
                    table.ForeignKey(
                        name: "FK__Funciones__IdSal__4AB81AF0",
                        column: x => x.IdSala,
                        principalTable: "Salas",
                        principalColumn: "IdSala");
                });

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    IdVenta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    FechaVenta = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Total = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    Estado = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "CONFIRMADA")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Ventas__BC1240BD7F9CFACC", x => x.IdVenta);
                    table.ForeignKey(
                        name: "FK__Ventas__IdUsuari__4D94879B",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "DetalleVenta",
                columns: table => new
                {
                    IdDetalle = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdVenta = table.Column<int>(type: "int", nullable: false),
                    IdFuncion = table.Column<int>(type: "int", nullable: false),
                    IdAsiento = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(6,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DetalleV__E43646A5B0F56608", x => x.IdDetalle);
                    table.ForeignKey(
                        name: "FK__DetalleVe__IdAsi__5441852A",
                        column: x => x.IdAsiento,
                        principalTable: "Asientos",
                        principalColumn: "IdAsiento");
                    table.ForeignKey(
                        name: "FK__DetalleVe__IdFun__534D60F1",
                        column: x => x.IdFuncion,
                        principalTable: "Funciones",
                        principalColumn: "IdFuncion");
                    table.ForeignKey(
                        name: "FK__DetalleVe__IdVen__52593CB8",
                        column: x => x.IdVenta,
                        principalTable: "Ventas",
                        principalColumn: "IdVenta");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asientos_IdSala",
                table: "Asientos",
                column: "IdSala");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVenta_IdAsiento",
                table: "DetalleVenta",
                column: "IdAsiento");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVenta_IdFuncion",
                table: "DetalleVenta",
                column: "IdFuncion");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVenta_IdVenta",
                table: "DetalleVenta",
                column: "IdVenta");

            migrationBuilder.CreateIndex(
                name: "IX_Funciones_IdPelicula",
                table: "Funciones",
                column: "IdPelicula");

            migrationBuilder.CreateIndex(
                name: "IX_Funciones_IdSala",
                table: "Funciones",
                column: "IdSala");

            migrationBuilder.CreateIndex(
                name: "UQ__Generos__75E3EFCF2E4AF571",
                table: "Generos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Peliculas_IdGenero",
                table: "Peliculas",
                column: "IdGenero");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdRol",
                table: "Usuarios",
                column: "IdRol");

            migrationBuilder.CreateIndex(
                name: "UQ__Usuarios__60695A198DBF786E",
                table: "Usuarios",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_IdUsuario",
                table: "Ventas",
                column: "IdUsuario");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleVenta");

            migrationBuilder.DropTable(
                name: "Asientos");

            migrationBuilder.DropTable(
                name: "Funciones");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropTable(
                name: "Peliculas");

            migrationBuilder.DropTable(
                name: "Salas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Generos");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
