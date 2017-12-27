# MultiBound - Starbound Profile Launcher
MultiBound allows you to set up separate profiles/"instances" with different saves, mod loadouts, etc.; perfect for separating your Frackin' Universe and non-FU saves, or what have you.

# How to use
### Windows
Just extract the zip in its own folder somewhere, edit `multibound.config` with your `starbound.exe` location if you installed it somewhere other than the default location, and run `MultiBound.exe`.

### Linux
**( tested on Manjaro; your package names may be different! )**
- Make sure you have `mono` and `gtk-sharp-3` installed in your package manager
- Extract the zip in its own folder somewhere
- Edit `multibound.config`, pointing `starboundPath` to your `run-client.sh`
- - (for example, mine is at `/home/zetaprime/.steam/steam/steamapps/common/Starbound/linux/run-client.sh`)
- `mono MultiBound.exe` and enjoy!

*( For compilation on \*nix systems, see [#1](https://github.com/zetaPRIME/MultiBound/issues/1) )*
