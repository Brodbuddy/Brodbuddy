CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE refresh_tokens
(
    id                   UUID PRIMARY KEY     DEFAULT uuid_generate_v4(),
    token                TEXT        NOT NULL,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at           TIMESTAMPTZ NOT NULL,
    revoked_at           TIMESTAMPTZ,
    replaced_by_token_id UUID REFERENCES refresh_tokens (id)
);

CREATE TABLE users
(
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email      VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ  NOT NULL
);

CREATE TABLE one_time_passwords
(
    id         UUID PRIMARY KEY     DEFAULT uuid_generate_v4(),
    code       INTEGER     NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMPTZ NOT NULL,
    is_used    BOOLEAN     NOT NULL DEFAULT FALSE
);

CREATE TABLE devices
(
    id           UUID PRIMARY KEY      DEFAULT uuid_generate_v4(),
    name         VARCHAR(255) NOT NULL,
    browser      VARCHAR(255) NOT NULL,
    os           VARCHAR(255) NOT NULL,
    created_at   TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_seen_at TIMESTAMPTZ  NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE device_registry
(
    id         UUID PRIMARY KEY     DEFAULT uuid_generate_v4(),
    user_id    UUID        NOT NULL REFERENCES users (id),
    device_id  UUID        NOT NULL REFERENCES devices (id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (user_id, device_id)
);

CREATE TABLE token_contexts
(
    id               UUID PRIMARY KEY     DEFAULT uuid_generate_v4(),
    user_id          UUID        NOT NULL REFERENCES users (id),
    device_id        UUID        NOT NULL REFERENCES devices (id),
    refresh_token_id UUID        NOT NULL REFERENCES refresh_tokens (id),
    created_at       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_revoked       BOOLEAN     NOT NULL DEFAULT FALSE,
    UNIQUE (refresh_token_id)
);

CREATE TABLE verification_contexts
(
    id         UUID PRIMARY KEY     DEFAULT uuid_generate_v4(),
    user_id    UUID        NOT NULL REFERENCES users (id),
    otp_id     UUID        NOT NULL REFERENCES one_time_passwords (id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (user_id, otp_id)
);

CREATE INDEX idx_verification_contexts_user_id ON verification_contexts (user_id);
