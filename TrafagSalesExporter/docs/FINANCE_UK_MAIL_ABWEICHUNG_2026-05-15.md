# Email an UK / England: Abweichung Net Sales 2025

**Subject:** Review difference Net Sales 2025 - UK

Hi,

In the Net Sales 2025 reconciliation for UK / England, we identified a difference against the Rhino / `check.xlsx` reference value.

Summary:

- Actual UK: `3,533,710.09 GBP`
- Rhino reference: `3,749,865.00 GBP`
- Reference UK LC/GBP: `3,538,972.00 GBP`
- Difference: `-5,261.91 GBP`

The mapping has already been reviewed technically, but we still need to clarify the remaining difference before closing the 2025 value.

Important clarification: UK / England is treated as a Sage source. The current SharePoint folder name `UK_B1` is only a technical folder/source reference and does not mean that UK is read from SAP Business One.

Could you please check the following points?

1. Full-year completeness  
   Confirm that the UK source file/import contains the full year 2025 and not only a partial period.

2. Period included  
   We currently see data from approximately `03.01.2025` to `22.12.2025`. Please confirm whether this is complete for 2025 or if transactions outside this range are missing.

3. Credit notes  
   Confirm that credit notes are included correctly and with the correct negative sign.

4. Net sales field  
   Confirm which column should be used as the net sales amount for comparison with Rhino / `check.xlsx`.

5. Discounts, freight or additional charges  
   Please check whether discounts, freight, additional charges or other adjustments are included in the Rhino reference but not in the current import value, or vice versa.

6. 2nd-party / 3rd-party / Intercompany  
   Confirm whether any customers or transactions should be excluded from the Net Sales 2025 value.

7. Currency  
   Confirm that the correct comparison currency for UK is `GBP`.

Goal: clarify whether the difference of `-216,154.91 GBP` is caused by an incomplete period, credit notes, a different net sales field, adjustments, or 2nd-party/3rd-party handling.

Thanks and best regards,
