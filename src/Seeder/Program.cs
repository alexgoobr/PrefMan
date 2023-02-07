using Microsoft.Extensions.DependencyInjection;
using PrefMan.Core.Interfaces;
using PrefMan.Infrastructure;
using PrefMan.Seeder;

Console.WriteLine("Starting PrefMan Seeder");
var preferencesRepository = Startup.ServiceProvider.GetRequiredService<IPreferenceMetadataRepository>();
var userPreferenceService = Startup.ServiceProvider.GetRequiredService<IPreferencesService>();
PreferencesSeeder seeder = new PreferencesSeeder(preferencesRepository, userPreferenceService);

await seeder.SeedPreferencesTable();
await seeder.SeedUserPreferencesTable();

Console.WriteLine("Seeding complete");
Thread.Sleep(5000);
