#![no_std]
#![no_main]

use esp_backtrace as _;
use esp_println::print;
use hal::{
    clock::ClockControl,
    gpio::{GpioPin, Input, Output, PullUp, PushPull, IO},
    peripherals::{Peripherals, UART0},
    prelude::*,
    Delay, Uart,
};

const BUFFER_SIZE: usize = 20;
const DEBOUNCE_DELAY_MS: u32 = 50;
const LED_PIN_NUMBER: u8 = 32;
const BUTTON_PIN_NUMBER: u8 = 34;

struct RingBufferArr<T: Copy, const N: usize> {
    buffer: [Option<T>; N],
    read_index: usize,
    write_index: usize,
}

impl<T: Copy + PartialEq, const N: usize> RingBufferArr<T, N> {
    // create a new RingBufferArr
    fn new() -> Self {
        RingBufferArr {
            buffer: [None; N],
            read_index: 0,
            write_index: 0,
        }
    }

    // add an item to the buffer
    fn push(&mut self, item: T) {
        self.buffer[self.write_index] = Some(item);
        self.write_index = (self.write_index + 1) % N;

        // if the buffer is full, move the read_index as well
        if self.write_index == self.read_index {
            self.read_index = (self.read_index + 1) % N;
        }
    }

    // remove and return an item from the buffer
    fn pop(&mut self) -> Option<T> {
        if self.is_empty() {
            None
        } else {
            let item: Option<T> = self.buffer[self.read_index].take();
            self.read_index = (self.read_index + 1) % N;
            item
        }
    }

    // check if the buffer is empty
    fn is_empty(&self) -> bool {
        self.read_index == self.write_index && self.buffer[self.read_index].is_none()
    }

    // check if the buffer is full
    fn _is_full(&self) -> bool {
        self.read_index == self.write_index && self.buffer[self.read_index].is_some()
    }

    // pop items until target is found or max items are popped and return array of options
    fn pop_until(&mut self, target: T, max_elements: usize) -> [Option<T>; N] {
        let mut popped_items: [Option<T>; N] = [None; N];
        let mut count: usize = 0;

        while !self.is_empty() && count < max_elements {
            if let Some(item) = self.pop() {
                popped_items[count] = Some(item);
                count += 1;
                if item == target {
                    break;
                }
            }
        }
        popped_items
    }
}

#[entry]
fn main() -> ! {
    // initialize or setup
    let peripherals = Peripherals::take();
    let system = peripherals.SYSTEM.split();
    let clocks = ClockControl::boot_defaults(system.clock_control).freeze();

    let io = IO::new(peripherals.GPIO, peripherals.IO_MUX);

    let mut serial = Uart::new(peripherals.UART0, &clocks);
    let mut delay = Delay::new(&clocks);

    let mut serial_read_buffer: RingBufferArr<char, BUFFER_SIZE> = RingBufferArr::new();
    let mut led = io.pins.gpio32.into_push_pull_output();
    let mut button = io.pins.gpio34.into_pull_up_input();

    let mut last_button_state: bool = button.is_low().unwrap();

    loop {
        handle_serial_input(&mut serial, &mut serial_read_buffer, &mut led);
        handle_button_press(&mut button, &mut last_button_state, &mut led, &mut delay);
    }
}

fn handle_serial_input(
    serial: &mut Uart<'_, UART0>,
    serial_read_buffer: &mut RingBufferArr<char, BUFFER_SIZE>,
    led: &mut GpioPin<Output<PushPull>, LED_PIN_NUMBER>,
) {
    if let Ok(read) = serial.read() {
        let read: char = read as char;

        serial_read_buffer.push(read);

        if read == '\0' {
            process_buffer(serial_read_buffer, led);
        }
    }
}

fn handle_button_press(
    button: &mut GpioPin<Input<PullUp>, BUTTON_PIN_NUMBER>,
    last_button_state: &mut bool,
    led: &mut GpioPin<Output<PushPull>, LED_PIN_NUMBER>,
    delay: &mut Delay,
) {
    let current_button_state = button.is_low().unwrap();

    // detect falling edge
    if *last_button_state && !current_button_state {
        led_toggle(led);
        delay.delay_ms(DEBOUNCE_DELAY_MS);
    }

    *last_button_state = current_button_state;
}

fn process_buffer(
    serial_read_buffer: &mut RingBufferArr<char, BUFFER_SIZE>,
    led: &mut GpioPin<Output<PushPull>, LED_PIN_NUMBER>,
) {
    let char_option_arr: [Option<char>; BUFFER_SIZE] =
        serial_read_buffer.pop_until('\0', BUFFER_SIZE);
    let mut char_arr: [u8; BUFFER_SIZE] = [0u8; BUFFER_SIZE];

    for (index, char_some) in char_option_arr.iter().enumerate().take(BUFFER_SIZE) {
        if let Some(char_char) = char_some {
            char_arr[index] = *char_char as u8;
        }
    }

    if let Ok(char_str) = core::str::from_utf8(&char_arr) {
        let trimmed_str = char_str
            .trim_matches(|c: char| c.is_whitespace() || c == '\r' || c == '\n' || c == '\0');

        match trimmed_str {
            "LED ON" => led_on(led),
            "LED OFF" => led_off(led),
            "LED TOGGLE" => led_toggle(led),
            _ => print!("Unrecognized command\0"), // debug print for unmatched commands
        }
    }
}

fn led_on(led: &mut GpioPin<Output<PushPull>, LED_PIN_NUMBER>) {
    if led.set_high().is_ok() {
        print!("LEDSTATE HIGH\0");
    } else {
        print!("Error setting LED high\0");
    }
}

fn led_off(led: &mut GpioPin<Output<PushPull>, LED_PIN_NUMBER>) {
    if led.set_low().is_ok() {
        print!("LEDSTATE LOW\0");
    } else {
        print!("Error setting LED low\0");
    }
}

fn led_toggle(led: &mut GpioPin<Output<PushPull>, LED_PIN_NUMBER>) {
    if led.toggle().is_ok() {
        if led.is_set_high().unwrap() {
            print!("LEDSTATE HIGH\0");
        } else {
            print!("LEDSTATE LOW\0");
        }
    } else {
        print!("Error toggling LED\0");
    }
}
