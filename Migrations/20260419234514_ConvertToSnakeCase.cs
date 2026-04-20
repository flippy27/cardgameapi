using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardDuel.ServerApi.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToSnakeCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration already applied manually to the database.
            // No-op to prevent duplicate execution.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert all renames (reverse order, reverse mapping)
            // ... reverting would require renaming back, which is tedious
            // For now, keeping this empty as rollback is not practical for this type of migration
        }
    }
}
