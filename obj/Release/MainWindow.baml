   M S B A M L   `   `   `  ���� G  CDigiHash, Version=1.0.0.22083, Culture=neutral, PublicKeyToken=null#clr-namespace:DigiHashDigiHash      DigiHash.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     +digihashclr-namespace:DigiHash       � Title$  DigiHash��$	��500��$	��700�� � WindowStartupLocation$ CenterScreen=�����  �� EndPoint$ 0.5,1&� � MappingMode$ RelativeToBoundingBox=� �� 
StartPoint$ 0.5,0&�.���  � Color$ Black�� � Offset$ 1Q�� # � �� � TaskbarItemInfo *  $System.Windows.Shell.TaskbarItemInfo  .���  � 	Resources    DigiHash.IsNullConverter   IsNullConverter	 �� 
TargetType  ReadySelector
 �� Property �� Value �� Triggers �� Binding �� Path �� Value  ComboBoxSelector �� BasedOn P� Property P� Value  MiningSelector �� ElementName�� %+  &	        &	      &	 �     7  &	 �      �� "	 ����� 
 � {� .  ��  �� $ 
Algorithms� Z� .a��� 
 � {� .  ��  �� $ Started� true.a��� 
 � {� .  �� "	 ��8   ��� 
 �@��
IsReadOnly . �� 
 �@��
IsEditable . �� 
 � ��$
 Auto���� 
 � ��$ Normal$��� 
 � ��
 �   ��� 
 �@��TextAlignment$ Justify=� P�  �@��SelectedItem Z� . ��� 
 � ��$ Italic$��� 
 � ��
 ������� 
 �@��TextAlignment$ Center=��� "	 ����� 
 � {� .  ��  �� $ WalletComboBox��$ SelectedItem� Z� .a��� 
 � {� .  ��  �� $ AlgorithmComboBox��$ SelectedItem� Z� .a��� 
 � {� .  ��  �� $ Started� true.a��� 
 � {� .   � RowDefinitions �� $
��Auto ��� $��* �.+���  �� Orientation$ 
Horizontal=�$��0��.+��� $��Profile��$��0,0,5,5q�$��0,12,0,0q�.���� .+��� -    ��Name$ WalletComboBox��$	��120�� �� DisplayMemberPath$
 Name�� �� SelectedValuePath$ Address������ $ Wallets� �� SelectedValue �� $ Profile.Wallet����� "	 ��# [  P�  �@��SelectedItem Z� . ��� 
 �@��Text$ 
--Wallet--���� -   $ AlgorithmComboBox��$	��120��$
 Name��$
 Name�� � UseLayoutRounding .  ���� $ 
Algorithms� �� $ Profile.Algorithm����� "	 ��# [  P�  �@��SelectedItem Z� . ��� 
 �@��Text$ --Algorithm--���� -   $��WalletButton��$��5q�#��[ ��Z� ���� .+��� $��Resources/wallet.png��$��64��$��64���� $��Wallet��$��Center=��� -   $��
StopButton��$��5q���Z� ���� "	 ����� 
 � {� .  ��  �� $ Miner� �� 	Converter# [   true.a��� 
 � {� .  ���� .+��� $��Resources/stop.png��$��64��$��64���� $
��Stop��$��Center=��� -    ��Name$ StartButton��$��5q�#��[ ��Z� ���� .+��� $��Resources/play.ico��$��64��$��64���� $��Start��$��Center=��� -   $��OutputRichTextBox��$��1�� �� IsUndoEnabled .   �� AutoWordSelection .   �� 
IsReadOnly . 
���   �
�������$��2q�$
��Auto=� ��%3   'J�       �� "	 J�.��� 
 �@��Margin$ 0q�