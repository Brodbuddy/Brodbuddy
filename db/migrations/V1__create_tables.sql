CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE one_time_passwords (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code INTEGER NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    is_used BOOLEAN DEFAULT FALSE NOT NULL
);