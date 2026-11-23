ALTER TABLE "Discs"
    ADD COLUMN "ReorderThreshold" integer NOT NULL DEFAULT 3,
    ADD CONSTRAINT "CK_Discs_ReorderThreshold_Nonnegative"
        CHECK ("ReorderThreshold" >= 0);

CREATE INDEX "IX_Discs_Quantity_ReorderThreshold"
    ON "Discs" ("Quantity", "ReorderThreshold");
