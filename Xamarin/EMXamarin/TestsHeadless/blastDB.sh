#!/bin/sh
echo '================================== waiting 5 seconds ================================'
sleep 5
echo '================================== cleaning mysql db ==============================='
/home/myrete/cassandra/bin/cqlsh  --color --debug --file=/home/myrete/staging/database/cassandra-truncate.cql
mysql -u em -pyBD90i567K5l09U3 < /home/myrete/staging/database/create_database-0.1.sql
rm ~/EMDatabaseTest*
if [ -d ~/media ]; then
	rm -r ~/media
fi
sleep 10
