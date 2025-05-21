CREATE TABLE features (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name             VARCHAR(100) NOT NULL UNIQUE,
    description      TEXT,
    is_enabled       BOOLEAN     NOT NULL DEFAULT false,
    rollout_percentage INTEGER   CHECK (rollout_percentage >= 0 AND rollout_percentage <= 100),
    created_at       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_modified_at TIMESTAMPTZ
);

CREATE TABLE feature_users (
    id         UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    feature_id UUID        NOT NULL REFERENCES features(id) ON DELETE CASCADE,
    user_id    UUID        NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(feature_id, user_id)
);

CREATE INDEX idx_features_name ON features (name);
CREATE INDEX idx_feature_users_feature_id ON feature_users(feature_id);
CREATE INDEX idx_feature_users_user_id ON feature_users(user_id);