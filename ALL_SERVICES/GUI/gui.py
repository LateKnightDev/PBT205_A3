import dearpygui.dearpygui as dpg
import threading
import pika
import rabbitmq_logic
import json
import queue #these are thread safe for safe board updates
import time
#------------------------------------------------------------------------------------

#colour palate
red = (229,169,169)
yellow = (234,231,170)
green = (178,214,170)
blue = (172,179,234)
purple = (212,186,219)
off_white = (238,238,210)
pure_white = (255,255,255,255)
black = (0,0,0,255)
text_red = (255,0,0,255)
light_grey = (224,224,224,255)

#sizes of stuff in gui
viewport_width= 1920   #1080p resolution
viewport_height= 1080
divisor = 2
send_order_width = 600
#-------------------------------------------------------------------------------------------
#gui fns

def check_empty(text_box):
    text_input = dpg.get_value(text_box)
    if text_input == "" or text_input == " ":
        dpg.set_value(text_box, "Field cannot be empty")
        return False
    return True

def check_int(text_box):
    text_input = dpg.get_value(text_box)
    try:
        text_input = int(text_input)
    except ValueError:
        dpg.set_value(text_box, "Can only be an integer")
        return False
    return True

def check_float(text_box):
    text_input = dpg.get_value(text_box)
    try:
        text_input = float(text_input)
    except ValueError:
        dpg.set_value(text_box, "Can only be a number")
        return False
    return True

#checks if any fields are empty first. if not, checks correct types
def check_correct_input(text_input_fields):
    for value in text_input_fields.values():
        if check_empty(value) == False:
            return False
    return check_int(text_input_fields["quantity"]) and check_float(text_input_fields["price"]) 

def on_click_send_order(sender, app_data, user_data):    
    if not check_correct_input(user_data):
        print("Incorrect field detected")
        return
    
    #decode the dict values and send to rabbit as json string
    send = []
    for value in user_data.values():
        send.append(dpg.get_value(value))
    send = json.dumps(send)
    print("sent message: ", send)
    send_order_publisher.publish("order", send.encode())

    #display success and block for a few seconds to stop spam
    dpg.set_value("success_text", "Success!")
    dpg.disable_item("send_button")
    time.sleep(3)
    dpg.set_value("success_text", "")
    dpg.enable_item("send_button")

#----------------------------------------------------------------------------------------------
#Gui setup

dpg.create_context()

#load the font
with dpg.font_registry():
    futuristic_font = dpg.add_font("assets/font.ttf", 30)
dpg.bind_font(futuristic_font)

#configure theme for visual appearance
with dpg.theme() as window_theme:
    #force window colours
    with dpg.theme_component(dpg.mvWindowAppItem):
        dpg.add_theme_color(dpg.mvThemeCol_TitleBg, (242,240,239,255)) #inactive title bar
        dpg.add_theme_color(dpg.mvThemeCol_TitleBgActive, (242,240,239,255)) #active title bar
        dpg.add_theme_style(dpg.mvStyleVar_FrameRounding, 5)
        
    with dpg.theme_component(dpg.mvAll):
        dpg.add_theme_color(dpg.mvThemeCol_Text,black) #black text
        dpg.add_theme_color(dpg.mvThemeCol_WindowBg, light_grey) #grey windows
        
        #button stuff
        dpg.add_theme_color(dpg.mvThemeCol_Button, (100, 150, 250, 255)) #blue buttons
        dpg.add_theme_color(dpg.mvThemeCol_ButtonHovered, (120, 170, 255, 255))
        dpg.add_theme_color(dpg.mvThemeCol_ButtonActive, (250, 197, 100, 255))
        dpg.add_theme_color(dpg.mvThemeCol_TextDisabled, (0, 60, 180, 255))
        dpg.add_theme_style(dpg.mvStyleVar_FrameRounding, 5)

    #text input theme
    with dpg.theme_component(dpg.mvInputText):
        dpg.add_theme_color(dpg.mvThemeCol_FrameBg, black, category=dpg.mvThemeCat_Core)
        dpg.add_theme_color(dpg.mvThemeCol_Text, pure_white, category=dpg.mvThemeCat_Core) #white text
        dpg.add_theme_style(dpg.mvStyleVar_FrameRounding, 5)

    #combo input
    with dpg.theme_component(dpg.mvCombo):
        dpg.add_theme_color(dpg.mvThemeCol_FrameBg, black) 
        dpg.add_theme_color(dpg.mvThemeCol_Text, pure_white)   #white text
        dpg.add_theme_style(dpg.mvStyleVar_FrameRounding, 5)   #rounded corners

#create the main window
dpg.create_viewport(title="Trades Visualiser", width=viewport_width//divisor, height=viewport_height//divisor)
dpg.setup_dearpygui()
dpg.show_viewport()

#---------------------------------------------------------------------------------------------
#subscriber stuff

#make the update queue for rabbit to write updates into
update_queue = queue.Queue()

# #subscriber responses here
# def respond(ch, method, properties, body):

#     # if method.routing_key == "query_response.board_state":
#     #     data = body.decode()
#     #     data = json.loads(data)
#     #     update_queue.put(data)
#     pass
    
# #main rabbit fn
# def rabbit_thread_logic():
#     sub = rabbitmq_logic.Subscriber()          #publish and subscribe
#     sub.publisher = rabbitmq_logic.Publisher()
    
#     sub.subscribe_to_queue("Orders", respond) #listen to Orders 
#     sub.subscribe_to_queue("Trades", respond) #listen to Orders
#     sub.start_listening() #blocks here. It's ok though because it's in another thread

# #start the rabbit thread here
# threading.Thread(target=rabbit_thread_logic, daemon=True).start()

#--------------------------------------------------------------------------------------------

def show_error():
    with dpg.window(label="Error", tag="error_window"):
        dpg.add_text("Cannot Establish Connection")
        dpg.bind_item_theme("error_window", window_theme)

def show_main_ui():
    with dpg.window(label="Send Order", tag="send_order", width=send_order_width):
        with dpg.group(horizontal=True):
            with dpg.group():
                dpg.add_text("Username:")
                dpg.add_text("Buy/Sell:")
                dpg.add_text("Quantity (#):")
                dpg.add_text("Price ($.¢¢):")
                dpg.add_text("Code:")
                dpg.add_button(label="Send", callback=on_click_send_order,
                               user_data={
                                   "username": "username_text",
                                   "side": "buy_sell_text",
                                   "quantity": "quantity_text",
                                   "price": "price_text",
                                   "code": "code_text"
                               },
                               tag="send_button")

            with dpg.group():
                dpg.add_input_text(tag="username_text")
                dpg.add_combo(tag="buy_sell_text", items=["Buy", "Sell"], default_value="Buy")
                dpg.add_input_text(tag="quantity_text")
                dpg.add_input_text(tag="price_text")
                dpg.add_input_text(tag="code_text")
                dpg.add_text("", tag="success_text")

    dpg.bind_item_theme("send_order", window_theme)

#try to connect to rabbit
try:
    send_order_publisher = rabbitmq_logic.Publisher()
    send_order_publisher.bind_queue("Orders", "order")
    show_main_ui()
except Exception as e:
    print(e)
    show_error()

#the imgui loop (like a gameloop). use to get updates from Thread Queue
while dpg.is_dearpygui_running():
    while not update_queue.empty():
        update = update_queue.get() 
    dpg.render_dearpygui_frame()
dpg.destroy_context() #clean up
send_order_publisher.end()