ALTER TABLE "Users"
    ADD CONSTRAINT "CK_Users_Role" CHECK ("Role" IN ('user', 'admin'));

CREATE INDEX "IX_Users_CreatedAt" ON "Users" ("CreatedAt");
