CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TABLE sourdough_analyzers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    mac_address VARCHAR(17) NOT NULL UNIQUE,
    name VARCHAR(255),
    firmware_version VARCHAR(50),
    activation_code VARCHAR(12) UNIQUE,
    is_activated BOOLEAN NOT NULL DEFAULT FALSE,
    activated_at TIMESTAMPTZ,
    last_seen TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_sourdough_analyzers_mac_address ON sourdough_analyzers(mac_address);
CREATE INDEX idx_sourdough_analyzers_activation_code ON sourdough_analyzers(activation_code);

CREATE TABLE user_analyzers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    analyzer_id UUID NOT NULL REFERENCES sourdough_analyzers(id) ON DELETE CASCADE,
    is_owner BOOLEAN DEFAULT TRUE,
    nickname VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, analyzer_id)
);

CREATE INDEX idx_user_analyzers_user_id ON user_analyzers(user_id);
CREATE INDEX idx_user_analyzers_analyzer_id ON user_analyzers(analyzer_id);

CREATE TRIGGER update_sourdough_analyzers_updated_at 
    BEFORE UPDATE ON sourdough_analyzers 
    FOR EACH ROW 
    EXECUTE FUNCTION update_updated_at_column();