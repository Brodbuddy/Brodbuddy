CREATE TABLE device_telemetry (
                                  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                                  device_id VARCHAR(255) NOT NULL,
                                  distance DOUBLE PRECISION NOT NULL,
                                  rise_percentage DOUBLE PRECISION NOT NULL,
                                  timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
                                  created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
