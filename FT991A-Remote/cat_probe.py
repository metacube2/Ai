#!/usr/bin/env python3
import argparse
import os
import select
import sys
import termios
import time


def configure_port(fd: int, baud: int, rts: bool) -> None:
    attrs = termios.tcgetattr(fd)
    attrs[0] &= ~(termios.IXON | termios.IXOFF | termios.IXANY | termios.ICRNL | termios.INLCR | termios.IGNBRK)
    attrs[1] &= ~termios.OPOST
    attrs[2] &= ~(termios.PARENB | termios.CSTOPB | termios.CSIZE | termios.CRTSCTS | termios.HUPCL)
    attrs[2] |= termios.CS8 | termios.CREAD | termios.CLOCAL
    attrs[3] &= ~(termios.ICANON | termios.ECHO | termios.ECHOE | termios.ISIG)
    speed = {
        4800: termios.B4800,
        9600: termios.B9600,
        19200: termios.B19200,
        38400: termios.B38400,
        57600: termios.B57600,
        115200: termios.B115200,
    }.get(baud, termios.B38400)
    attrs[4] = speed
    attrs[5] = speed
    attrs[6][termios.VMIN] = 0
    attrs[6][termios.VTIME] = 10
    termios.tcsetattr(fd, termios.TCSANOW, attrs)

    clear_bits = int(termios.TIOCM_RTS | termios.TIOCM_DTR)
    fcntl_ioctl(fd, termios.TIOCMBIC, clear_bits)
    if rts:
        set_bits = int(termios.TIOCM_RTS)
        fcntl_ioctl(fd, termios.TIOCMBIS, set_bits)


def fcntl_ioctl(fd: int, op: int, value: int) -> None:
    import fcntl
    import struct
    fcntl.ioctl(fd, op, struct.pack("I", value))


def read_until(fd: int, timeout: float) -> bytes:
    end = time.time() + timeout
    data = bytearray()
    while time.time() < end:
        remaining = max(0.01, end - time.time())
        ready, _, _ = select.select([fd], [], [], remaining)
        if not ready:
            continue
        chunk = os.read(fd, 512)
        if not chunk:
            continue
        data.extend(chunk)
        if b";" in chunk:
            break
    return bytes(data)


def probe(port: str, baud: int, rts: bool, commands: list[str], timeout: float) -> int:
    fd = os.open(port, os.O_RDWR | os.O_NOCTTY | os.O_NONBLOCK)
    try:
        configure_port(fd, baud, rts)
        print(f"PORT {port} baud={baud} rts={'on' if rts else 'off'}")
        os.write(fd, b"")
        termios.tcflush(fd, termios.TCIOFLUSH)
        for command in commands:
            payload = command.encode("ascii")
            print(f"TX {command}")
            os.write(fd, payload)
            data = read_until(fd, timeout)
            if data:
                print(f"RX {data.decode('ascii', 'ignore').strip()}")
            else:
                print("RX <none>")
            time.sleep(0.15)
        return 0
    finally:
        os.close(fd)


def main() -> int:
    parser = argparse.ArgumentParser(description="Minimal CAT probe for FT-991A serial ports")
    parser.add_argument("port", nargs="+", help="Serial port(s) to test, e.g. /dev/cu.usbserial-0082AAF10")
    parser.add_argument("--baud", type=int, default=38400)
    parser.add_argument("--rts", action="store_true")
    parser.add_argument("--timeout", type=float, default=0.7)
    parser.add_argument("--commands", nargs="*", default=["ID;", "FA;", "IF;"])
    args = parser.parse_args()

    rc = 0
    for port in args.port:
        try:
            rc |= probe(port, args.baud, args.rts, args.commands, args.timeout)
        except Exception as exc:
            rc = 1
            print(f"ERROR {port}: {exc}", file=sys.stderr)
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
