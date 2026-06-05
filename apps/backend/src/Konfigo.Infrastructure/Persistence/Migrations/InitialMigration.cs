using System;
using FluentMigrator;
using Konfigo.Infrastructure.Persistence.Migrations.Shared;

namespace Konfigo.Infrastructure.Persistence.Migrations;

[Migration(1, "InitialMigration")]
internal sealed class InitialMigration : SqlMigration
{
    protected override string GetUpSql(IServiceProvider services)
    {
        return @"
CREATE TABLE public.application_services (
    id uuid NOT NULL PRIMARY KEY,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    name text NOT NULL,
    description text NULL,
    repository_url text NULL,
    gitlab_project_id text NULL,
    contact_email text NULL,
    num int NOT NULL GENERATED ALWAYS AS IDENTITY
);

CREATE TABLE public.config_versions (
    id uuid NOT NULL PRIMARY KEY,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    service_id uuid NOT NULL,
    version_label text NOT NULL,
    description text NULL
);

CREATE UNIQUE INDEX idx__config_versions__service_id__version_label ON public.config_versions (service_id, version_label);

ALTER TABLE public.config_versions
    ADD CONSTRAINT fk__config_versions__application_services
    FOREIGN KEY (service_id)
    REFERENCES public.application_services (id)
    ON DELETE CASCADE;

CREATE TABLE public.config_entries (
    id uuid NOT NULL PRIMARY KEY,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    config_version_id uuid NOT NULL,
    key text NOT NULL,
    raw_value text NULL,
    value_type integer NOT NULL,
    enum_definition text NULL,
    description text NULL,
    group_name text NULL,
    group_description text NULL,
    generation int NOT NULL,
    name text NOT NULL
);

CREATE INDEX idx__config_entries__config_version_id ON public.config_entries (config_version_id);

ALTER TABLE public.config_entries
    ADD CONSTRAINT fk__config_entries__config_versions
    FOREIGN KEY (config_version_id)
    REFERENCES public.config_versions (id)
    ON DELETE CASCADE;

CREATE TABLE public.audit_logs (
    id uuid NOT NULL PRIMARY KEY,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    num int NOT NULL GENERATED ALWAYS AS IDENTITY,
    service_id uuid NOT NULL,
    user_id text NULL,
    entry jsonb NOT NULL
);
";
    }

    protected override string GetDownSql(IServiceProvider services)
    {
        return @"
DROP TABLE public.audit_logs;
DROP TABLE public.config_entries;
DROP TABLE public.config_versions;
DROP TABLE public.application_services;
";
    }
}
