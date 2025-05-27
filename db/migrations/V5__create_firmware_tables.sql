CREATE TABLE firmware_versions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    version VARCHAR(20) NOT NULL,
    description TEXT NOT NULL,
    file_size BIGINT NOT NULL,
    crc32 BIGINT NOT NULL,
    file_url TEXT,
    release_notes TEXT,
    is_stable BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by UUID REFERENCES users(id)
);

CREATE TABLE firmware_updates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    analyzer_id UUID NOT NULL REFERENCES sourdough_analyzers(id),
    firmware_version_id UUID NOT NULL REFERENCES firmware_versions(id),
    status VARCHAR(20) NOT NULL, 
    progress INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ,
    CONSTRAINT status_check CHECK (status IN ('started', 'downloading', 'applying', 'complete', 'failed'))
);

CREATE INDEX idx_firmware_versions_version ON firmware_versions(version);
CREATE INDEX idx_firmware_versions_created_at ON firmware_versions(created_at DESC);
CREATE INDEX idx_firmware_updates_analyzer_id ON firmware_updates(analyzer_id);
CREATE INDEX idx_firmware_updates_status ON firmware_updates(status);
CREATE INDEX idx_firmware_updates_started_at ON firmware_updates(started_at DESC);

CREATE TABLE analyzer_readings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    analyzer_id UUID NOT NULL REFERENCES sourdough_analyzers(id) ON DELETE CASCADE,
    epoch_time BIGINT NOT NULL,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    timestamp TIMESTAMPTZ NOT NULL,
    local_time TIMESTAMP NOT NULL,
    temperature DECIMAL(10,2),
    humidity DECIMAL(5,2),
    rise DECIMAL(10,2),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_analyzer_readings_analyzer_id ON analyzer_readings(analyzer_id);
CREATE INDEX idx_analyzer_readings_user_id ON analyzer_readings(user_id);
CREATE INDEX idx_analyzer_readings_timestamp ON analyzer_readings(timestamp);
CREATE INDEX idx_analyzer_readings_epoch_time ON analyzer_readings(epoch_time);
CREATE INDEX idx_analyzer_readings_created_at ON analyzer_readings(created_at);