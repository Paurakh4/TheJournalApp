using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TheJournalApp.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<JournalDbContext>
{
    public JournalDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<JournalDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=thejournalapp;Username=paurakh;Include Error Detail=true");

        return new JournalDbContext(optionsBuilder.Options);
    }
}
