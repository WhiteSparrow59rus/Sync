using System.Linq;
using LuchIntegrationEF.Objects.Custom.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LuchIntegrationEF.SyncService.Data
{

    public class DataContext : IdentityDbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Territory> Territories
        {
            get;
            set;
        }

        public virtual DbSet<DeviceModel> DeviceModels
        {
            get;
            set;
        }

        public virtual DbSet<DeviceType> DeviceTypes
        {
            get;
            set;
        }

        public virtual DbSet<Facility> Facilities
        {
            get;
            set;
        }

        public virtual DbSet<Device> Devices
        {
            get;
            set;
        }

        public virtual DbSet<InstantValue> InstantValues
        {
            get;
            set;
        }

        public virtual DbSet<Unit> Units
        {
            get;
            set;
        }

        public virtual DbSet<Calibration> Calibrations
        {
            get;
            set;
        }

        public virtual DbSet<Channel> Channels
        {
            get;
            set;
        }

        public virtual DbSet<Indication> Indications
        {
            get;
            set;
        }

        public virtual DbSet<Consumption> Consumptions
        {
            get;
            set;
        }
        
        public virtual DbSet<AddressType> AddressTypes
        {
            get;
            set;
        }

        public virtual DbSet<AddressLevel> AddressLevels
        {
            get;
            set;
        }

        public virtual DbSet<Contractor> Contractors
        {
            get;
            set;
        }

        public virtual DbSet<ContractorRole> ContractorRoles
        {
            get;
            set;
        }

        public virtual DbSet<Event> Events
        {
            get;
            set;
        }

        public virtual DbSet<EventType> EventTypes
        {
            get;
            set;
        }

        public virtual DbSet<Protocol> Protocols
        {
            get;
            set;
        }

        public virtual DbSet<TimeZone> TimeZones
        {
            get;
            set;
        }

        public virtual DbSet<Balance> Balances
        {
            get;
            set;
        }

        public virtual DbSet<BalanceChannel> BalanceChannels
        {
            get;
            set;
        }

        public virtual DbSet<BalanceHistory> BalanceHistories
        {
            get;
            set;
        }

        public virtual DbSet<BalanceHistoryDetailed> BalanceHistoryDetailed
        {
            get;
            set;
        }

        public virtual DbSet<ReportType> ReportTypes
        {
            get;
            set;
        }

        public virtual DbSet<ReportVersion> ReportVersions
        {
            get;
            set;
        }

        public virtual DbSet<Report> Reports
        {
            get;
            set;
        }

        public virtual DbSet<ReportFile> ReportFiles
        {
            get;
            set;
        }

        public virtual DbSet<PowerProfile> PowerProfiles
        {
            get;
            set;
        }

        public virtual DbSet<Command> Commands
        {
            get;
            set;
        }

        public void DetachAllEntities()
        {
            var changedEntriesCopy = this.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added ||
                            e.State == EntityState.Modified ||
                            e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in changedEntriesCopy)
                entry.State = EntityState.Detached;
        }
    }
}
