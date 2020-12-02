### [**DEPRECATED - MultiBound 2 has been released**](https://github.com/zetaPRIME/MultiBound2)
MultiBound 2 is a full rewrite, replacing the crust of GTK# with Qt5 (with probably a lower memory footprint), and can also install/update Steam Workshop mods for you without having to launch Steam (or indeed even have the Steam version).

---
# MultiBound - Starbound Profile Launcher
MultiBound allows you to set up separate profiles/"instances" with different saves, mod loadouts, etc.; perfect for separating your Frackin' Universe and non-FU saves, or what have you.

# How to use
### Windows
Just extract the zip in its own folder somewhere, edit `multibound.config` with your `starbound.exe` location if you installed it somewhere other than the default location, and run `MultiBound.exe`.

### Linux
**Linux users are recommended to use MultiBound 2 instead (see above)**

**( tested on Arch; your package names may be different! )**
- Make sure you have `mono` and `gtk-sharp-3` installed in your package manager
- Extract the zip in its own folder somewhere
- Edit `multibound.config`, pointing `starboundPath` to your `run-client.sh`
- - (for example, mine is at `/home/zetaprime/.steam/steam/steamapps/common/Starbound/linux/run-client.sh`)
- `mono MultiBound.exe` and enjoy!

*( For compilation on \*nix systems, see [#1](https://github.com/zetaPRIME/MultiBound/issues/1) )*
