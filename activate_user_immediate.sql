-- Immediate SQL script to activate user zsuzs@gmail.com
-- Copy and paste this into your SQLite database tool

UPDATE T_USER 
SET ACTIVE = 1,
    MODIFICATIONDATE = datetime('now')
WHERE EMAIL = 'zsuzs@gmail.com';

-- Verify activation
SELECT USERID, EMAIL, FIRSTNAME, LASTNAME, ACTIVE 
FROM T_USER 
WHERE EMAIL = 'zsuzs@gmail.com';

