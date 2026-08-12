using System;
using FluentMigrator;
using Konfigo.Infrastructure.Persistence.Migrations.Shared;

namespace Konfigo.Infrastructure.Persistence.Migrations;

[Migration(2, "AddLocalUsers")]
internal sealed class AddLocalUsersMigration : SqlMigration
{
    protected override string GetUpSql(IServiceProvider services)
    {
        return @"
CREATE TABLE public.local_users (
    username text NOT NULL PRIMARY KEY,
    password_hash text NOT NULL,
    role text NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL
);
";
    }

    protected override string GetDownSql(IServiceProvider services)
    {
        return @"
DROP TABLE public.local_users;
";
    }
}
