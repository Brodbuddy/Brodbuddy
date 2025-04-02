CREATE EXTENSION IF NOT EXISTS "uuid-ossp";


CREATE TABLE oneTimePassword (
                                 id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
                                 code INTEGER NOT NULL,
                                 createdAt TIMESTAMPTZ DEFAULT (CURRENT_TIMESTAMP),
                                 expiresAt TIMESTAMPTZ DEFAULT (CURRENT_TIMESTAMP + INTERVAL '15 minutes'),
                                 isUsed BOOLEAN DEFAULT FALSE
);