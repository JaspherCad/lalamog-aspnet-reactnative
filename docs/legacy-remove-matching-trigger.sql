-- OPTIONAL: PostgreSQL only. Run manually against the retired legacy database
-- after backing it up. The old API relies on this trigger and will stop matching
-- once it is removed. The SQL Server rewrite never uses this database.
BEGIN;
DROP TRIGGER IF EXISTS on_swipe_action ON public."SwipeActions";
DROP FUNCTION IF EXISTS public.check_mutual_swipe();
COMMIT;
