namespace BlueQ.Bot
{
    using System.Configuration;
    using Autofac;
    using BotAssets;
    using BotAssets.Dialogs;
    using Dialogs;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Internals.Fibers;
    using Microsoft.Bot.Builder.Location;
    using Microsoft.Bot.Builder.Scorables;
    using Microsoft.Bot.Connector;
    using Services.Models;

    public class BlueQHelpdeskModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<ParticipantLocationDialogFactory>()
                .Keyed<IParticipantLocationDialogFactory>(FiberModule.Key_DoNotSerialize)
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterType<RootDialog>()
                .As<IDialog<object>>()
                .InstancePerDependency();

            builder.RegisterType<SettingsScorable>()
                .As<IScorable<IActivity, double>>()
                .InstancePerLifetimeScope();

            builder.RegisterType<HelpCategoryDialog>()
                .InstancePerDependency();

            builder.RegisterType<HelpItemSelectionDialog>()
                .InstancePerDependency();

            builder.RegisterType<SavedParticipantLocationDialog>()
              .InstancePerDependency();

            builder.RegisterType<SettingsDialog>()
             .InstancePerDependency();

            // Location Dialog
            // ctor signature: LocationDialog(string apiKey, string channelId, string prompt, LocationOptions options = LocationOptions.None, LocationRequiredFields requiredFields = LocationRequiredFields.None, LocationResourceManager resourceManager = null);
            builder.RegisterType<LocationDialog>()
                .WithParameter("apiKey", ConfigurationManager.AppSettings["MicrosoftBingMapsKey"])
                .WithParameter("options", LocationOptions.UseNativeControl | LocationOptions.ReverseGeocode)
                .WithParameter("requiredFields", LocationRequiredFields.StreetAddress | LocationRequiredFields.Locality | LocationRequiredFields.Country)
                .WithParameter("resourceManager", new BotAssets.LocationResourceManager())
                .InstancePerDependency();

            // Service dependencies
            builder.RegisterType<Services.InMemoryHelpdeskTicketService>()
                .Keyed<Services.IHelpdeskTicketService>(FiberModule.Key_DoNotSerialize)
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<Services.InMemoryHelpItemRepository>()
                .Keyed<Services.IRepository<HelpItem>>(FiberModule.Key_DoNotSerialize)
                .AsImplementedInterfaces()
                .SingleInstance();

            builder.RegisterType<Services.InMemoryHelpCategoryRepository>()
                .Keyed<Services.IRepository<HelpCategory>>(FiberModule.Key_DoNotSerialize)
                .AsImplementedInterfaces()
                .SingleInstance();
        }
    }
}