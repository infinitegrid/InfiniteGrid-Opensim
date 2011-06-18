Currency DLL

<--done already
//cd (PREFIX)/opensim-X.Y.Z-source  (or opensim-X.Y.Z-release)
//tar xzfv opensim.currency-X.Y.Z.tar.gz
//patch -p1 < opensim.currency-X.Y.Z/opensim_currency_X.Y.Z.patch
-->

./runprebuild.sh && nant clean && nant 
cd opensim.currency-X.Y.Z
./build.sh
cd ../bin
vi MoneyServer.ini (not needed for distro, only moneyserver)
cp currency dll into /bin

Copy ossearch and osprofile DLLs into /bin
building not required, never tested

run ./nsl.modules/build.sh
cp NSL mute dll into /bin
