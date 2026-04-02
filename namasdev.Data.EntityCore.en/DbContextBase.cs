using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using namasdev.Core.Entity;
using namasdev.Core.Validation;

namespace namasdev.Data.EntityCore
{
    public class DbContextBase : DbContext
    {
        private readonly string _nameOrConnectionString;
        private readonly int? _commandTimeout;

        public DbContextBase(DbContextOptions options) 
            : base(options) 
        { 
        }

        public DbContextBase(
            string nameOrConnectionString,
            int? commandTimeout = null)
            : base() 
        {
            Validator.ValidateRequiredArgumentAndThrow(nameOrConnectionString, nameof(nameOrConnectionString));
            
            _nameOrConnectionString = nameOrConnectionString;
            _commandTimeout = commandTimeout;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseSqlServer(_nameOrConnectionString, o => o.CommandTimeout(_commandTimeout));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }

        public void Attach<T>(T entity, EntityState state,
            string[] propertiesToExcludeInUpdate = null)
            where T : class
        {
            Set<T>().Attach(entity);

            var entry = Entry(entity);
            entry.State = state;

            if (state == EntityState.Modified)
            {
                if (entity is IEntityCreated 
                    && propertiesToExcludeInUpdate == null)
                {
                    propertiesToExcludeInUpdate = new[]
                    {
                        nameof(IEntityCreated.CreatedBy),
                        nameof(IEntityCreated.CreatedAt),
                    };
                }

                SetPropertiesModifiedState(entry, propertiesToExcludeInUpdate, isModified: false);
            }
        }

        public void AttachModifiedProperties<T>(T entity, string[] properties)
            where T : class
        {
            Set<T>().Attach(entity);
            SetPropertiesModifiedState(Entry(entity), properties);
        }

        public void SetPropertiesModifiedState<T>(EntityEntry<T> entry, string[] properties,
            bool isModified = true)
            where T : class
        {
            if (properties != null)
            {
                foreach (string p in properties)
                {
                    entry.Property(p).IsModified = isModified;
                }
            }
        }

        public TResult ExecuteQueryAndGet<TResult>(string query,
            object[] parameters = null)
        {
            return Database
                .SqlQueryRaw<TResult>(query, parameters ?? Array.Empty<object>())
                .FirstOrDefault();
        }

        public async Task<TResult> ExecuteQueryAndGetAsync<TResult>(string query,
            object[] parameters = null,
            CancellationToken ct = default)
        {
            return await Database
                .SqlQueryRaw<TResult>(query, parameters ?? Array.Empty<object>())
                .FirstOrDefaultAsync(ct);
        }

        public List<TResult> ExecuteQueryAndGetList<TResult>(string query,
            object[] parameters = null)
        {
            return Database
                .SqlQueryRaw<TResult>(query, parameters ?? Array.Empty<object>())
                .ToList();
        }

        public async Task<List<TResult>> ExecuteQueryAndGetListAsync<TResult>(string query,
            object[] parameters = null,
            CancellationToken ct = default)
        {
            return await Database
                .SqlQueryRaw<TResult>(query, parameters ?? Array.Empty<object>())
                .ToListAsync(ct);
        }

        public void ExecuteCommand(string command,
            object[] parameters = null)
        {
            Database.ExecuteSqlRaw(command, parameters ?? Array.Empty<object>());
        }

        public async Task ExecuteCommandAsync(string command,
            object[] parameters = null,
            CancellationToken ct = default)
        {
            await Database.ExecuteSqlRawAsync(command, parameters ?? Array.Empty<object>(), ct);
        }

        public TResult ExecuteCommandAndGet<TResult>(string command,
            Func<DbDataReader, TResult> recordMap,
            IEnumerable<DbParameter> parameters = null)
            where TResult : class
        {
            TResult result = null;

            ExecuteCommandUsingNewConnection(
                command,
                cmd =>
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        result = recordMap(reader);
                    }
                },
                parameters: parameters);

            return result;
        }

        public async Task<TResult> ExecuteCommandAndGetAsync<TResult>(string command,
            Func<DbDataReader, Task<TResult>> recordMap,
            IEnumerable<DbParameter> parameters = null,
            CancellationToken ct = default)
            where TResult : class
        {
            TResult result = null;

            await ExecuteCommandUsingNewConnectionAsync(
                command,
                async cmd =>
                {
                    using (var reader = await cmd.ExecuteReaderAsync(ct))
                    {
                        result = await recordMap(reader);
                    }
                },
                parameters: parameters,
                ct: ct);

            return result;
        }

        private void ExecuteCommandUsingNewConnection(string command, Action<DbCommand> action,
            IEnumerable<DbParameter> parameters = null)
        {
            using (var cmd = CreateCommand(command, parameters))
            {
                try
                {
                    Database.OpenConnection();
                    action(cmd);
                }
                finally
                {
                    Database.CloseConnection();
                }
            }
        }

        private async Task ExecuteCommandUsingNewConnectionAsync(string command, Func<DbCommand, Task> action,
            IEnumerable<DbParameter> parameters = null,
            CancellationToken ct = default)
        {
            using (var cmd = CreateCommand(command, parameters))
            {
                try
                {
                    await Database.OpenConnectionAsync(ct);
                    await action(cmd);
                }
                finally
                {
                    await Database.CloseConnectionAsync();
                }
            }
        }

        private DbCommand CreateCommand(string command, 
            IEnumerable<DbParameter> parameters = null)
        {
            var cmd = Database.GetDbConnection().CreateCommand();
            cmd.CommandText = command;

            if (_commandTimeout.HasValue)
                cmd.CommandTimeout = _commandTimeout.Value;

            if (parameters != null)
                cmd.Parameters.AddRange(parameters.ToArray());

            return cmd;
        }
    }
}
