#include <AsyncPrinter.h>
#include <async_config.h>
#include <ESPAsyncTCP.h>
#include <ESPAsyncTCPbuffer.h>
#include <SyncClient.h>
#include <tcp_axtls.h>
#include <WiFiManager.h>
#include <ESP8266WiFi.h>


extern "C" {
#include <osapi.h>
#include <os_type.h>
}

#include "config.h"

#define SERVER_HOST_NAME "192.168.0.20"
#define TCP_PORT 8888

static os_timer_t intervalTimer;

AsyncClient* client;
bool pressed;
char message[32];

char *uint8tob( uint8_t value ) {
  static uint8_t base = 2;
  static char buffer[8] = {0};

  int i = 8;
  for( ; i ; --i, value /= base ) {
    buffer[i] = "01"[value % base];
  }

  return &buffer[i+1];
}

char *convert_bytes_to_binary_string( uint8_t *bytes, size_t count ) {
  if ( count < 1 ) {
    return NULL;
  }

  size_t buffer_size = 8 * count + 1;
  char *buffer = (char*)calloc( 1, buffer_size );
  if ( buffer == NULL ) {
    return NULL;
  }

  char *output = buffer;
  for ( int i = 0 ; i < count ; i++ ) {
    memcpy( output, uint8tob( bytes[i] ), 8 );
    output += 8;
  }
  return buffer;
}

void doServerReply(char* Message)
{
 
  strcat(message, "poop ");
  strcat(message, Message);
  replyToServer(client);
}
static void replyToServer(void* arg) 
{
  AsyncClient* client = reinterpret_cast<AsyncClient*>(arg);
  if (client->space() > 32 && client->canSend())
  {
    char msg[strlen("Received") + 1];
    msg[0] = 3;
    strcat(msg, "Received");
    client->add(msg, strlen(msg));
    client->send();
    memset(msg, 0, strlen(msg));
    return;
  }
}


static uint8_t getHeading(void *data)
{
  uint8_t header;
  memmove(&header, data, sizeof(uint8_t));
  return header;
}



/* event callbacks */
static void handleData(void* arg, AsyncClient* client, void *data, size_t len) 
{
   
   Serial.println(getHeading(data));
   Serial.write((uint8_t*)data, len);
   replyToServer(client);

}
 
void onConnect(void* arg, AsyncClient* client) 
{

}


void setup() {

  Serial.begin(115200);
  WiFiManager wifiManager;
  wifiManager.setAPStaticIPConfig(IPAddress(10,0,1,1), IPAddress(10,0,1,1), IPAddress(255,255,255,0));
  wifiManager.autoConnect("Stuff");

  Serial.println(WiFi.localIP());
  client = new AsyncClient;
  client->onData(&handleData, client);
  client->onConnect(&onConnect, client);
  client->connect(SERVER_HOST_NAME, TCP_PORT);
  os_timer_disarm(&intervalTimer);
  os_timer_setfn(&intervalTimer, &replyToServer, client);

}

void refreshClient()
{
  client = NULL;
  delete client;
  client = new AsyncClient;
  client->onData(&handleData, client);
  client->onConnect(&onConnect, client);
  client->connect(SERVER_HOST_NAME, TCP_PORT);
  os_timer_disarm(&intervalTimer);
  os_timer_setfn(&intervalTimer, &replyToServer, client);
}

void reverse(char* str, int len) 
{ 
    int i = 0, j = len - 1, temp; 
    while (i < j) { 
        temp = str[i]; 
        str[i] = str[j]; 
        str[j] = temp; 
        i++; 
        j--; 
    } 
} 

void ftoa(float n, char* res, int afterpoint) 
{ 
    // Extract integer part 
    int ipart = (int)n; 
  
    // Extract floating part 
    float fpart = n - (float)ipart; 
  
    // convert integer part to string 
    int i = intToStr(ipart, res, 0); 
  
    // check for display option after point 
    if (afterpoint != 0) { 
        res[i] = '.'; // add dot 
  
        // Get the value of fraction part upto given no. 
        // of points after dot. The third parameter  
        // is needed to handle cases like 233.007 
        fpart = fpart * pow(10, afterpoint); 
  
        intToStr((int)fpart, res + i + 1, afterpoint); 
    } 
} 

int intToStr(int x, char str[], int d) 
{ 
    int i = 0; 
    while (x) { 
        str[i++] = (x % 10) + '0'; 
        x = x / 10; 
    } 
  
    // If number of digits required is more, then 
    // add 0s at the beginning 
    while (i < d) 
        str[i++] = '0'; 
  
    reverse(str, i); 
    str[i] = '\0'; 
    return i; 
} 

void loop() 
{
  
 
}