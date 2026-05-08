-- Ensure Location column exists and is correctly sized in Candidates table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Candidates') AND name = 'Location')
BEGIN
    ALTER TABLE Candidates ADD Location NVARCHAR(255) NULL;
END
ELSE
BEGIN
    ALTER TABLE Candidates ALTER COLUMN Location NVARCHAR(255) NULL;
END

-- Ensure Location column exists and is correctly sized in Recruiters table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Recruiters') AND name = 'Location')
BEGIN
    ALTER TABLE Recruiters ADD Location NVARCHAR(255) NULL;
END
ELSE
BEGIN
    ALTER TABLE Recruiters ALTER COLUMN Location NVARCHAR(255) NULL;
END
