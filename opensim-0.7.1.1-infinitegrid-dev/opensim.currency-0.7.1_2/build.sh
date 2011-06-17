#!/bin/bash

CONFIGPATH=./config
OPNSIMPATH=../bin

echo "=========================="
echo "NSL_CURRENCY"
echo "=========================="

rm -f OpenSim.Data.MySQL.MySQLMoneyDataWrapper/OpenSim.Data.MySQL.MySQLMoneyDataWrapper.dll
rm -f OpenSim.Forge.Currency/OpenSim.Forge.Currency.dll
rm -f OpenSim.Grid.MoneyServer/OpenSim.Grid.MoneyServer.exe

(cd OpenSim.Data.MySQL.MySQLMoneyDataWrapper/ && nant && cp OpenSim.Data.MySQL.MySQLMoneyDataWrapper.dll ../$OPNSIMPATH)
(cd OpenSim.Forge.Currency/ && nant && cp OpenSim.Forge.Currency.dll ../$OPNSIMPATH)
(cd OpenSim.Grid.MoneyServer/ && nant && cp OpenSim.Grid.MoneyServer.exe ../$OPNSIMPATH)


if [ ! -f $OPNSIMPATH/MoneyServer.ini ]; then
	cp $CONFIGPATH/MoneyServer.ini $OPNSIMPATH
else
	cp $CONFIGPATH/MoneyServer.ini $OPNSIMPATH/MoneyServer.ini.example
fi

if [ ! -f $OPNSIMPATH/OpenSim.Grid.MoneyServer.exe.config ]; then
	cp $CONFIGPATH/OpenSim.Grid.MoneyServer.exe.config $OPNSIMPATH
fi

if [ ! -f $OPNSIMPATH/SineWaveCert.pfx ]; then
	cp $CONFIGPATH/SineWaveCert.pfx $OPNSIMPATH
fi
