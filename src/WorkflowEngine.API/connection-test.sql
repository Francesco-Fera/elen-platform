CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(320) NOT NULL,
    "PasswordHash" text NOT NULL,
    "FirstName" character varying(100),
    "LastName" character varying(100),
    "IsActive" boolean NOT NULL,
    "IsEmailVerified" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastLoginAt" timestamp with time zone,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);


CREATE TABLE "RefreshTokens" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "TokenHash" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "RevokedAt" timestamp with time zone,
    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "Workflows" (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Description" character varying(1000),
    "Status" text NOT NULL,
    "NodesJson" jsonb,
    "ConnectionsJson" jsonb,
    "SettingsJson" jsonb,
    "Version" integer NOT NULL,
    "CreatedBy" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastModified" timestamp with time zone,
    CONSTRAINT "PK_Workflows" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Workflows_Users_CreatedBy" FOREIGN KEY ("CreatedBy") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "WorkflowExecutions" (
    "Id" uuid NOT NULL,
    "WorkflowId" uuid NOT NULL,
    "UserId" uuid,
    "Status" text NOT NULL,
    "TriggerType" text NOT NULL,
    "InputDataJson" jsonb,
    "OutputDataJson" jsonb,
    "ErrorDataJson" jsonb,
    "StartedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "Duration" interval,
    CONSTRAINT "PK_WorkflowExecutions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_WorkflowExecutions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE SET NULL,
    CONSTRAINT "FK_WorkflowExecutions_Workflows_WorkflowId" FOREIGN KEY ("WorkflowId") REFERENCES "Workflows" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ExecutionLogs" (
    "Id" uuid NOT NULL,
    "ExecutionId" uuid NOT NULL,
    "Level" text NOT NULL,
    "Message" text NOT NULL,
    "NodeId" text,
    "DataJson" jsonb,
    "Timestamp" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ExecutionLogs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ExecutionLogs_WorkflowExecutions_ExecutionId" FOREIGN KEY ("ExecutionId") REFERENCES "WorkflowExecutions" ("Id") ON DELETE CASCADE
);


CREATE INDEX "IX_ExecutionLogs_ExecutionId" ON "ExecutionLogs" ("ExecutionId");


CREATE INDEX "IX_ExecutionLogs_Level" ON "ExecutionLogs" ("Level");


CREATE INDEX "IX_ExecutionLogs_NodeId" ON "ExecutionLogs" ("NodeId");


CREATE INDEX "IX_ExecutionLogs_Timestamp" ON "ExecutionLogs" ("Timestamp");


CREATE INDEX "IX_RefreshTokens_ExpiresAt" ON "RefreshTokens" ("ExpiresAt");


CREATE INDEX "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");


CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");


CREATE INDEX "IX_Users_CreatedAt" ON "Users" ("CreatedAt");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


CREATE INDEX "IX_Users_IsActive" ON "Users" ("IsActive");


CREATE INDEX "IX_WorkflowExecutions_StartedAt" ON "WorkflowExecutions" ("StartedAt");


CREATE INDEX "IX_WorkflowExecutions_Status" ON "WorkflowExecutions" ("Status");


CREATE INDEX "IX_WorkflowExecutions_UserId" ON "WorkflowExecutions" ("UserId");


CREATE INDEX "IX_WorkflowExecutions_WorkflowId" ON "WorkflowExecutions" ("WorkflowId");


CREATE INDEX "IX_Workflows_CreatedAt" ON "Workflows" ("CreatedAt");


CREATE INDEX "IX_Workflows_CreatedBy" ON "Workflows" ("CreatedBy");


CREATE INDEX "IX_Workflows_Name" ON "Workflows" ("Name");


CREATE INDEX "IX_Workflows_Status" ON "Workflows" ("Status");


