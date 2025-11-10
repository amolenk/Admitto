START TRANSACTION;
CREATE TABLE email_recipient_lists (
    id uuid NOT NULL,
    event_id uuid NOT NULL,
    name character varying(100) NOT NULL,
    created_at timestamp with time zone NOT NULL,
    last_changed_at timestamp with time zone NOT NULL,
    recipients jsonb,
    CONSTRAINT "PK_email_recipient_lists" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX "IX_email_recipient_lists_name" ON email_recipient_lists (name);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251109103102_Add EmailRecipientLists', '9.0.8');

COMMIT;

