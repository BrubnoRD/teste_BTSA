using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransferenciasFinanceiras.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarChavesEstrangeirasERestricoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Transferencias_Contas_Diferentes",
                table: "Transferencias",
                sql: "\"IdContaOrigem\" <> \"IdContaDestino\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Transferencias_Valor_Positivo",
                table: "Transferencias",
                sql: "\"Valor\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Contas_LimiteChequeEspecial_NaoNegativo",
                table: "Contas",
                sql: "\"LimiteChequeEspecial\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Contas_Saldo_DentroDoChequeEspecial",
                table: "Contas",
                sql: "\"Saldo\" >= -\"LimiteChequeEspecial\"");

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Contas_IdContaDestino",
                table: "Transferencias",
                column: "IdContaDestino",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transferencias_Contas_IdContaOrigem",
                table: "Transferencias",
                column: "IdContaOrigem",
                principalTable: "Contas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Contas_IdContaDestino",
                table: "Transferencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Transferencias_Contas_IdContaOrigem",
                table: "Transferencias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transferencias_Contas_Diferentes",
                table: "Transferencias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Transferencias_Valor_Positivo",
                table: "Transferencias");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Contas_LimiteChequeEspecial_NaoNegativo",
                table: "Contas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Contas_Saldo_DentroDoChequeEspecial",
                table: "Contas");
        }
    }
}
