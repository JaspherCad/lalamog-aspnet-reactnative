-- Check which columns exist in FightSchedules table
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'FightSchedules' 
AND table_schema = 'public'
ORDER BY column_name;

-- Check which columns exist in Profiles table  
SELECT column_name, data_type, is_nullable
FROM information_schema.columns 
WHERE table_name = 'Profiles' 
AND table_schema = 'public'
ORDER BY column_name;
