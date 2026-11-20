ALTER TABLE "Discs"
    ADD CONSTRAINT "CK_Discs_Price_Positive" CHECK ("Price" > 0),
    ADD CONSTRAINT "CK_Discs_Quantity_Nonnegative" CHECK ("Quantity" >= 0);

ALTER TABLE "Sales"
    ADD CONSTRAINT "CK_Sales_Quantity_Positive" CHECK ("Quantity" > 0),
    ADD CONSTRAINT "CK_Sales_TotalAmount_Nonnegative" CHECK ("TotalAmount" >= 0);
