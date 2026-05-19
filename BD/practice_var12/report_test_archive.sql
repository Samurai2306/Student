-- Тест триггера архивации (отдельный файл — не прерывает основной прогон)
\set ON_ERROR_STOP on
\pset border 2
\pset format aligned

DELETE FROM authors_archive;

UPDATE authors
   SET passport_data = '4501 111111 (обновлено)'
 WHERE author_id = 1;

SELECT archive_id, author_id, old_full_name, old_passport_data, change_timestamp
FROM authors_archive
ORDER BY archive_id;
