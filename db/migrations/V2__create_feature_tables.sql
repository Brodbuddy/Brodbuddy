CREATE TABLE features (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(100) NOT NULL UNIQUE,
    description     TEXT,
    is_enabled      BOOLEAN NOT NULL DEFAULT false,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_modified_at TIMESTAMPTZ
);

CREATE INDEX idx_features_name ON features (name);

INSERT INTO features (name, description, is_enabled) 
VALUES 
('Api.Test.Ping', 'kakao', true),
('Api.TestAuth.GetUser', 'Enable login', false);