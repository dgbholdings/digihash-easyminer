   M S B A M L   `   `   `  ����   DigiHash    DigiHash.WalletDialog   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml       � WindowStyle$  
ToolWindow=� � Title$ Wallet��$	��400��$	��500�� � WindowStartupLocation$ CenterScreen=�.��� $��5q� � 	Resources  �� 
TargetType   InputSelector �� Property �� Value �� Triggers �� Binding	 �� Path
 �� Value�� %k   &	        �� " �����  � {� .  ��  �� $	 Wallet�
 Z� .a���  � {� .   � RowDefinitions �� $��* ��� $��25 � � ColumnDefinitions �� $	��150 ��� $��* �.+��� -    ��Name$ WalletListBox��$
��Auto��$��0��$��0�� �� DisplayMemberPath$
 Name��$��0,0,0,5q����� $	 Wallets�� $��0��$��1�� �� $��30 ��� $��5 ��� $��30 ��� $��5 ��� $��25 ��� $��* � �� $��60 ��� $��* �.+��� -   $��NameTextBox��$��Name:��|�. $��0��$��0��$��5,5,0,0q��� $��0��$��1��#��[  $��0,5,0,0q����� $	 Wallet.Name� �� Mode$ TwoWay=��� $��Address:��$��2��$��0��$��5,5,0,0q��� $��2��$��1��#��[  $��0,5,0,0q����� $	 Wallet.Address�$ TwoWay=���  �� Orientation$ 
Horizontal=�$��Right=�$��4��$��1��.+��� -    ��Name$ 
SaveButton��$
��Save��$��50��#��[  �� $ 
Horizontal=�$��1��$��0��.+��� -   $��	AddButton��$	��Add��$��50��$��0,0,5,0q��� -   $��RemoveButton��$��Remove��$��50��$��0,0,5,0q�#��[  �� $ 
Horizontal=�$��Right=�$��1��$��1��.+��� -   $ CloseButton��$��Close��$��50��