using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reflecta.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTagCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "tags",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Seed categories for existing system tags
            migrationBuilder.Sql(@"
                UPDATE tags SET category = 'Personal' WHERE is_system = true AND LOWER(name) IN ('gratitude', 'reflection', 'dreams', 'goals', 'memories');
                UPDATE tags SET category = 'Health' WHERE is_system = true AND LOWER(name) IN ('health', 'fitness', 'meditation', 'sleep', 'nutrition', 'wellness', 'exercise');
                UPDATE tags SET category = 'Work' WHERE is_system = true AND LOWER(name) IN ('work', 'career', 'productivity', 'meeting', 'project', 'deadline');
                UPDATE tags SET category = 'Travel' WHERE is_system = true AND LOWER(name) IN ('travel', 'vacation', 'adventure', 'trip');
                UPDATE tags SET category = 'Relationships' WHERE is_system = true AND LOWER(name) IN ('family', 'friends', 'relationships', 'love', 'social');
                UPDATE tags SET category = 'Lifestyle' WHERE is_system = true AND LOWER(name) IN ('hobby', 'reading', 'music', 'creative', 'learning', 'nature');
                UPDATE tags SET category = 'Personal' WHERE is_system = true AND category IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "tags");
        }
    }
}
