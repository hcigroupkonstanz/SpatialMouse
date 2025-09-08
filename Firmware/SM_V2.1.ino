#include <BleMouse.h>
BleMouse bleMouse;
#include <SPI.h>
#include <pgmspace.h>

// Registers
#define Product_ID 0x00
#define Revision_ID 0x01
#define Motion 0x02
#define Delta_X_L 0x03
#define Delta_X_H 0x04
#define Delta_Y_L 0x05
#define Delta_Y_H 0x06
#define SQUAL 0x07
#define Raw_Data_Sum 0x08
#define Maximum_Raw_data 0x09
#define Minimum_Raw_data 0x0A
#define Shutter_Lower 0x0B
#define Shutter_Upper 0x0C
#define Control 0x0D
#define Resolution_L 0x0E
#define Resolution_H 0x0F
#define Config2 0x10
#define Angle_Tune 0x11
#define Frame_Capture 0x12
#define SROM_Enable 0x13
#define Run_Downshift 0x14
#define Rest1_Rate_Lower 0x15
#define Rest1_Rate_Upper 0x16
#define Rest1_Downshift 0x17
#define Rest2_Rate_Lower 0x18
#define Rest2_Rate_Upper 0x19
#define Rest2_Downshift 0x1A
#define Rest3_Rate_Lower 0x1B
#define Rest3_Rate_Upper 0x1C
#define Observation 0x24
#define Data_Out_Lower 0x25
#define Data_Out_Upper 0x26
#define Raw_Data_Dump 0x29
#define SROM_ID 0x2A
#define Min_SQ_Run 0x2B
#define Raw_Data_Threshold 0x2C
#define Config5 0x2F
#define Power_Up_Reset 0x3A
#define Shutdown 0x3B
#define Inverse_Product_ID 0x3F
#define LiftCutoff_Tune3 0x41
#define Angle_Snap 0x42
#define LiftCutoff_Tune1 0x4A
#define Motion_Burst 0x50
#define LiftCutoff_Tune_Timeout 0x58
#define LiftCutoff_Tune_Min_Length 0x5A
#define SROM_Load_Burst 0x62
#define Lift_Config 0x63
#define Raw_Data_Burst 0x64
#define LiftCutoff_Tune2 0x65

// Set this to what pin your "INT0" hardware interrupt feature is on
#define Motion_Interrupt_Pin 2

unsigned long lastScrollTime = 0; // Track the last scroll time
const unsigned long scrollDelay = 70;

const int lift_delay_sensitivity = 500; // > 0 means mouse in the air
int counter = 0;

const int ncs = 3;  // SPI "slave select" pin that the sensor is hooked up to

byte initComplete = 0;
volatile int xydat[2];
volatile byte movementflag = 0;

extern const unsigned short firmware_length;
extern const unsigned char firmware_data[];

const int buttonLeft = 6;
const int buttonRight = 7;

int VRxPin = A0;  // X-axis
int VRyPin = A1;  // Y-axis
//int SWPin = 4;   //For thumbstick click

int buttonStateLeft = 0;
int buttonStateRight = 0;

void UpdatePointer();

void setup() {
  Serial.begin(9600);

  // Wait for the Serial connection only if it's connected (when running from the IDE)
  if (Serial) {
    while (!Serial.availableForWrite())
      ;
  }

  pinMode(A7, OUTPUT);

  digitalWrite(A7, HIGH);

  bleMouse.begin();

  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(buttonLeft, INPUT_PULLUP);
  pinMode(buttonRight, INPUT_PULLUP);

  //For thumbstick click
  //pinMode(SWPin, INPUT_PULLUP);

  pinMode(ncs, OUTPUT);

  pinMode(Motion_Interrupt_Pin, INPUT_PULLUP);
  digitalWrite(Motion_Interrupt_Pin, HIGH);

  SPI.begin();
  SPI.setDataMode(SPI_MODE3);
  SPI.setBitOrder(MSBFIRST);
  SPI.setClockDivider(SPI_CLOCK_DIV8);

  performStartup();

  delay(5000);

  dispRegisters();
  initComplete = 9;
}


void adns_com_begin() {
  digitalWrite(ncs, LOW);
}

void adns_com_end() {
  digitalWrite(ncs, HIGH);
}

byte adns_read_reg(byte reg_addr) {
  adns_com_begin();

  // Send address of the register, with MSBit = 0 to indicate it's a read
  SPI.transfer(reg_addr & 0x7f);
  delayMicroseconds(50);  // tSRAD
  // Read data
  byte data = SPI.transfer(0);

  delayMicroseconds(1);  // tSCLK-NCS for read operation is 120ns
  adns_com_end();
  delayMicroseconds(19);  //  tSRW/tSRR (=20us) minus tSCLK-NCS

  return data;
}

void adns_write_reg(byte reg_addr, byte data) {
  adns_com_begin();

  // Send address of the register, with MSBit = 1 to indicate it's a write
  SPI.transfer(reg_addr | 0x80);
  // Send data
  SPI.transfer(data);

  delayMicroseconds(20);  // tSCLK-NCS for write operation
  adns_com_end();
  delayMicroseconds(30);  // tSWW/tSWR (=120us) minus tSCLK-NCS. Could be shortened, but is looks like a safe lower bound
}

void adns_upload_firmware() {
  // Send the firmware to the chip, cf p.18 of the datasheet
  Serial.println("Uploading firmware...");

  // Write 0 to Rest_En bit of Config2 register to disable Rest mode.
  adns_write_reg(Config2, 0x00);

  // Write 0x1d in SROM_enable reg for initializing
  adns_write_reg(SROM_Enable, 0x1d);

  // Wait for more than one frame period
  delay(10);  // Assume that the frame rate is as low as 100fps... even if it should never be that low

  // Write 0x18 to SROM_enable to start SROM download
  adns_write_reg(SROM_Enable, 0x18);

  // Write the SROM file (=firmware data)
  adns_com_begin();
  SPI.transfer(SROM_Load_Burst | 0x80);  // Write burst destination address
  delayMicroseconds(15);

  // Send all bytes of the firmware
  unsigned char c;
  for (int i = 0; i < firmware_length; i++) {
    c = (unsigned char)pgm_read_byte(firmware_data + i);
    SPI.transfer(c);
    delayMicroseconds(15);
  }

  // Read the SROM_ID register to verify the ID before any other register reads or writes.
  adns_read_reg(SROM_ID);

  // Write 0x00 to Config2 register for wired mouse or 0x20 for wireless mouse design.
  adns_write_reg(Config2, 0x00);

  // Set initial CPI resolution
  //adns_write_reg(Config1, 0x15);

  // // Set CPI
  // byte cpi_low = adns_read_reg(Resolution_L);
  // byte cpi_high = adns_read_reg(Resolution_H);
  // uint16_t cpi = ((uint16_t)cpi_high << 8) + cpi_low;
  // Serial.print("CPI: ");
  // Serial.println(cpi);

  // uint16_t new_cpi = 2048;
  // adns_write_reg(Resolution_L, lowByte(new_cpi));
  // adns_write_reg(Resolution_H, highByte(new_cpi));

  // cpi_low = adns_read_reg(Resolution_L);
  // cpi_high = adns_read_reg(Resolution_H);
  // cpi = ((uint16_t)cpi_high << 8) + cpi_low;
  // Serial.print("New CPI: ");
  // Serial.println(cpi);

  adns_com_end();
}

void performStartup() {

  adns_com_end();                        // Ensure that the serial port is reset
  adns_com_begin();                      // Ensure that the serial port is reset
  adns_com_end();                        // Ensure that the serial port is reset
  adns_write_reg(Power_Up_Reset, 0x5a);  // Force reset
  delay(50);                             // Wait for it to reboot
  // Read registers 0x02 to 0x06 (and discard the data)
  adns_read_reg(Motion);
  adns_read_reg(Delta_X_L);
  adns_read_reg(Delta_X_H);
  adns_read_reg(Delta_Y_L);
  adns_read_reg(Delta_Y_H);
  // Upload the firmware
  // adns_upload_firmware();
  adns_write_reg(Rest1_Rate_Lower, 0x00);  // Disable Rest1 mode
  adns_write_reg(Rest1_Rate_Upper, 0x00);
  adns_write_reg(Rest1_Downshift, 0x00);

  adns_write_reg(Rest2_Rate_Lower, 0x00);  // Disable Rest2 mode
  adns_write_reg(Rest2_Rate_Upper, 0x00);
  adns_write_reg(Rest2_Downshift, 0x00);

  adns_write_reg(Rest3_Rate_Lower, 0x00);  // Disable Rest3 mode
  adns_write_reg(Rest3_Rate_Upper, 0x00);

  delay(1000);
  Serial.println("Optical Chip Initialized");
}

void UpdatePointer() {

  if (initComplete == 9) {

    // Write 0x01 to Motion register and read from it to freeze the motion values and make them available
    adns_write_reg(Motion, 0x01);
    adns_read_reg(Motion);

    xydat[0] = (int)adns_read_reg(Delta_X_L);
    xydat[1] = (int)adns_read_reg(Delta_Y_L);

    movementflag = 1;
  }
}

void dispRegisters() {
  int oreg[7] = { 0x00, 0x3F, 0x2A, 0x02 };
  char* oregname[] = { "Product_ID", "Inverse_Product_ID", "SROM_Version", "Motion" };
  byte regres;

  digitalWrite(ncs, LOW);

  int rctr = 0;
  for (rctr = 0; rctr < 4; rctr++) {
    SPI.transfer(oreg[rctr]);
    delay(1);
    Serial.println("---");
    Serial.println(oregname[rctr]);
    Serial.println(oreg[rctr], HEX);
    regres = SPI.transfer(0);
    Serial.println(regres, BIN);
    Serial.println(regres, HEX);
    delay(1);
  }
  digitalWrite(ncs, HIGH);
}

int convTwosComp(int b) {
  // Convert from 2's complement
  if (b & 0x80) {
    b = -1 * ((b ^ 0xff) + 1);
  }
  return b;
}

void mouseInAir() {
  // Serial.println("Mouse is in the air");
  bleMouse.press(MOUSE_MIDDLE);

  //VR trigger
  if (buttonStateLeft == LOW) {
    if (!bleMouse.isPressed(MOUSE_BACK)) {
      Serial.println("VR trigger");
      bleMouse.press(MOUSE_BACK);
    }
  } else {
    if (bleMouse.isPressed(MOUSE_BACK)) {
      bleMouse.release(MOUSE_BACK);
    }
  }

  //VR reset
  if (buttonStateRight == LOW) {
    if (!bleMouse.isPressed(MOUSE_FORWARD)) {
      Serial.println("VR reset");
      bleMouse.press(MOUSE_FORWARD);
    }
  } else {
    if (bleMouse.isPressed(MOUSE_FORWARD)) {
      bleMouse.release(MOUSE_FORWARD);
    }
  }
}

void mouseOnGround() {

  // Serial.println("Mouse is on the ground");
  bleMouse.release(MOUSE_MIDDLE);

  if (movementflag) {
    movementflag = 0;

    int x_movement = convTwosComp(xydat[0]);
    int y_movement = convTwosComp(xydat[1]);

    if (x_movement != 0 || y_movement != 0) {
      bleMouse.move(x_movement, y_movement);
    }
  }

  //mouse left click
  if (buttonStateLeft == LOW) {
    if (!bleMouse.isPressed(MOUSE_LEFT)) {
      Serial.println("Left click");
      bleMouse.press(MOUSE_LEFT);
      // bleKeyboard.print("H");
    }
  } else {
    if (bleMouse.isPressed(MOUSE_LEFT)) {
      bleMouse.release(MOUSE_LEFT);
    }
  }

  //mouse right click
  if (buttonStateRight == LOW) {
    if (!bleMouse.isPressed(MOUSE_RIGHT)) {
      Serial.println("Right click");
      bleMouse.press(MOUSE_RIGHT);
    }
  } else {
    if (bleMouse.isPressed(MOUSE_RIGHT)) {
      bleMouse.release(MOUSE_RIGHT);
    }
  }
}

void loop() {

  if (bleMouse.isConnected()) {

    UpdatePointer();

    unsigned long currentTime = millis(); // Get the current time

    buttonStateLeft = digitalRead(buttonLeft);
    buttonStateRight = digitalRead(buttonRight);

    // Read X-axis and Y-axis analog values
    int xValue = analogRead(VRxPin);  // X-axis value (0-4095 for ESP32)
    int yValue = analogRead(VRyPin);  // Y-axis value (0-4095 for ESP32)

    int centerY = 1960;
    int deadZone = 30; 
    int deviationY = yValue - centerY;

    if (abs(deviationY) < deadZone) {
      deviationY = 0;
    }

    int scrollSpeed = 0;
    if (deviationY != 0) {
      scrollSpeed = map(abs(deviationY), deadZone, 1960, 0, 2);
      if (deviationY < 0) {
        scrollSpeed = -scrollSpeed;
      }
    }

    // Perform scrolling with delay logic
    if (scrollSpeed != 0 && currentTime - lastScrollTime >= scrollDelay) {
      bleMouse.move(0, 0, scrollSpeed);  // Move mouse scroll
      lastScrollTime = currentTime; // Update the last scroll time
    }

    int squal = adns_read_reg(SQUAL);

    if (squal < 6) {
      if (counter <= lift_delay_sensitivity) {
        counter += 1;
      }

      if (counter > 0) {
        mouseInAir();
      }

    } else {
      if (counter >= -lift_delay_sensitivity) {
        counter -= 1;
      }

      if (counter < 0) {
        mouseOnGround();
      }
    }
  }
}
