using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransferenciasFinanceiras.Api.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Contas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeTitular = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Saldo = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LimiteChequeEspecial = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LimiteTransferenciaDiurno = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxTentativasPorHoraDiurno = table.Column<int>(type: "integer", nullable: false),
                    LimiteTransferenciaNoturno = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxTentativasPorHoraNoturno = table.Column<int>(type: "integer", nullable: false),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transferencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdContaOrigem = table.Column<Guid>(type: "uuid", nullable: false),
                    IdContaDestino = table.Column<Guid>(type: "uuid", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AgendadaPara = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MotivoFalha = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transferencias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_IdContaDestino",
                table: "Transferencias",
                column: "IdContaDestino");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_IdContaOrigem",
                table: "Transferencias",
                column: "IdContaOrigem");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_IdContaOrigem_Status_ProcessadaEm",
                table: "Transferencias",
                columns: new[] { "IdContaOrigem", "Status", "ProcessadaEm" });

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_Status_AgendadaPara",
                table: "Transferencias",
                columns: new[] { "Status", "AgendadaPara" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contas");

            migrationBuilder.DropTable(
                name: "Transferencias");
        }
    }
}
